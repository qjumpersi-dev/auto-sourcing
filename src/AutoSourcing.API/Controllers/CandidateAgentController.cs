using AutoSourcing.Data;
using AutoSourcing.Services.Agent;
using AutoSourcing.Services.Scotty;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoSourcing.API.Controllers;

public class CandidateChatRequest
{
    public string Message { get; set; } = string.Empty;
}

[ApiController]
[Route("api/agent")]
public class CandidateAgentController : ControllerBase
{
    private readonly AutoSourcingDbContext _dbContext;
    private readonly IScottyClient _scottyClient;
    private readonly ICandidateAgentService _agentService;

    public CandidateAgentController(AutoSourcingDbContext dbContext, IScottyClient scottyClient, ICandidateAgentService agentService)
    {
        _dbContext = dbContext;
        _scottyClient = scottyClient;
        _agentService = agentService;
    }

    [HttpGet("{leadId:int}")]
    public async Task<ContentResult> ChatPage(int leadId, CancellationToken cancellationToken)
    {
        var lead = await _dbContext.Leads.FindAsync([leadId], cancellationToken);
        if (lead is null)
        {
            return Content("<h1>Not found</h1>", "text/html");
        }

        if (lead.ConversationKey is null)
        {
            lead.ConversationKey = Guid.NewGuid();
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var org = await _dbContext.OrganizationProfiles.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        var orgName = org?.OrgName ?? "our team";
        var firstName = lead.FirstName;

        var html = $$"""
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1" />
              <title>Chat with {{orgName}}</title>
              <style>
                * { box-sizing: border-box; }
                body { font-family: system-ui, -apple-system, sans-serif; margin: 0; background: #f3f4f6; color: #111827; }
                .wrap { max-width: 560px; margin: 0 auto; height: 100vh; display: flex; flex-direction: column; background: #fff; }
                header { padding: 1rem 1.25rem; border-bottom: 1px solid #e5e7eb; }
                header h1 { font-size: 1rem; margin: 0; }
                header p { margin: .25rem 0 0; font-size: .8rem; color: #6b7280; }
                #messages { flex: 1; overflow-y: auto; padding: 1rem; display: flex; flex-direction: column; gap: .75rem; }
                .msg { max-width: 80%; padding: .6rem .8rem; border-radius: .75rem; font-size: .9rem; white-space: pre-wrap; }
                .agent { background: #f3f4f6; align-self: flex-start; }
                .user { background: #2563eb; color: #fff; align-self: flex-end; }
                footer { border-top: 1px solid #e5e7eb; padding: .75rem; display: flex; gap: .5rem; }
                #input { flex: 1; padding: .6rem; border: 1px solid #d1d5db; border-radius: .5rem; font-size: .9rem; }
                button { background: #2563eb; color: #fff; border: none; border-radius: .5rem; padding: .6rem 1rem; font-size: .9rem; font-weight: 600; cursor: pointer; }
                button:disabled { opacity: .5; }
              </style>
            </head>
            <body>
              <div class="wrap">
                <header>
                  <h1>{{orgName}}</h1>
                  <p>Hi {{firstName}} — ask me anything about the role or the company.</p>
                </header>
                <div id="messages"></div>
                <footer>
                  <input id="input" placeholder="Type a message..." autocomplete="off" />
                  <button id="send">Send</button>
                </footer>
              </div>
              <script>
                const leadId = {{leadId}};
                const messages = document.getElementById('messages');
                const input = document.getElementById('input');
                const sendBtn = document.getElementById('send');

                function addMessage(role, text) {
                  const div = document.createElement('div');
                  div.className = 'msg ' + role;
                  div.textContent = text;
                  messages.appendChild(div);
                  messages.scrollTop = messages.scrollHeight;
                }

                async function send() {
                  const text = input.value.trim();
                  if (!text) return;
                  input.value = '';
                  addMessage('user', text);
                  sendBtn.disabled = true;
                  try {
                    const res = await fetch('/api/agent/' + leadId + '/chat', {
                      method: 'POST',
                      headers: { 'Content-Type': 'application/json' },
                      body: JSON.stringify({ message: text })
                    });
                    const data = await res.json();
                    addMessage('agent', data.output || 'Sorry, I could not respond just now.');
                  } catch (e) {
                    addMessage('agent', 'Sorry, something went wrong.');
                  } finally {
                    sendBtn.disabled = false;
                    input.focus();
                  }
                }

                sendBtn.addEventListener('click', send);
                input.addEventListener('keydown', (e) => { if (e.key === 'Enter') send(); });
                input.focus();
              </script>
            </body>
            </html>
            """;

        return Content(html, "text/html");
    }

    [HttpPost("{leadId:int}/chat")]
    public async Task<IActionResult> Chat(int leadId, [FromBody] CandidateChatRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { error = "Message is required." });
        }

        var lead = await _dbContext.Leads.FindAsync([leadId], cancellationToken);
        if (lead is null)
        {
            return NotFound();
        }

        if (lead.ConversationKey is null)
        {
            lead.ConversationKey = Guid.NewGuid();
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        await _agentService.SaveMessageAsync(leadId, "user", request.Message, cancellationToken: cancellationToken);

        var result = await _scottyClient.SendTextAsync(new ScottyChatRequest
        {
            UserPrompt = request.Message,
            ContinuityKey = lead.ConversationKey.Value.ToString()
        }, cancellationToken);

        var output = result.Output ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(output))
        {
            await _agentService.SaveMessageAsync(leadId, "agent", output, cancellationToken: cancellationToken);
        }

        return Ok(new { output });
    }

    [HttpPost("{leadId:int}/call")]
    public async Task<IActionResult> Call(int leadId, CancellationToken cancellationToken)
    {
        var lead = await _dbContext.Leads.FindAsync([leadId], cancellationToken);
        if (lead is null)
        {
            return NotFound();
        }

        if (lead.ConversationKey is null)
        {
            lead.ConversationKey = Guid.NewGuid();
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var result = await _scottyClient.GetCallCredentialAsync(new ScottyCallRequest
        {
            SessionParticipantId = Guid.NewGuid().ToString(),
            ContinuityKey = lead.ConversationKey.Value.ToString()
        }, cancellationToken);

        return Ok(result);
    }
}
