using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace AutoSourcing.Services.LinkedIn;

public class PlaywrightLinkedInService : ILinkedInService
{
    private readonly LinkedInOptions _options;
    private readonly ILogger<PlaywrightLinkedInService> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IPlaywright? _playwright;
    private IBrowserContext? _context;

    public PlaywrightLinkedInService(IOptions<LinkedInOptions> options, ILogger<PlaywrightLinkedInService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<bool> IsSignedInAsync(CancellationToken cancellationToken = default)
        => RunWithContextResetAsync(() => IsSignedInCoreAsync(cancellationToken), cancellationToken);

    public Task<bool> SignInAsync(CancellationToken cancellationToken = default)
        => RunWithContextResetAsync(() => SignInCoreAsync(cancellationToken), cancellationToken);

    public Task<LinkedInSendResult> SendInMailAsync(string profileUrl, string subject, string body, CancellationToken cancellationToken = default)
        => RunWithContextResetAsync(() => SendInMailCoreAsync(profileUrl, subject, body, cancellationToken), cancellationToken);

    public Task<object?> ProbeDomAsync(string url, CancellationToken cancellationToken = default)
        => RunWithContextResetAsync(() => ProbeDomCoreAsync(url, cancellationToken), cancellationToken);

    private async Task<object?> ProbeDomCoreAsync(string url, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        var page = await context.NewPageAsync();
        try
        {
            await page.GotoAsync(string.IsNullOrWhiteSpace(url) ? "https://www.linkedin.com/feed/" : url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = _options.ActionTimeoutMs
            });
            await page.WaitForTimeoutAsync(4000);

            return await page.EvaluateAsync<dynamic>(
                "() => { const all = [...document.querySelectorAll('button, a, [role=button]')]; const pick = (re) => all.filter(e => (e.getAttribute('aria-label') || '').match(re) || (e.innerText || '').trim().match(re)).map(e => ({ tag: e.tagName, text: (e.innerText || '').trim().slice(0, 30), label: e.getAttribute('aria-label') || '' })).slice(0, 10); return { url: location.href, title: document.title, loginInput: !!document.querySelector('#username'), navCount: document.querySelectorAll('nav').length, message: pick(/message|inmail/i), connect: pick(/connect/i) }; }");
        }
        finally
        {
            await ClosePageAsync(page);
        }
    }

    public async Task<IReadOnlyList<LinkedInPageInfo>> GetOpenPagesAsync(CancellationToken cancellationToken = default)
    {
        var context = await GetContextAsync();
        var pages = new List<LinkedInPageInfo>();
        foreach (var page in context.Pages)
        {
            string url;
            try
            {
                url = page.Url;
            }
            catch
            {
                url = "(unavailable)";
            }

            string title;
            try
            {
                title = await page.TitleAsync();
            }
            catch
            {
                title = string.Empty;
            }

            bool navPrimary;
            bool navGlobal;
            string bodySample;
            try
            {
                var probe = await page.EvaluateAsync<dynamic>(
                    "() => ({ primary: !!document.querySelector('.global-nav__primary-items'), global: !!document.querySelector('nav.global-nav'), body: (document.body.innerText || '').slice(0, 80) })");
                navPrimary = probe.primary;
                navGlobal = probe.global;
                bodySample = probe.body;
            }
            catch
            {
                navPrimary = false;
                navGlobal = false;
                bodySample = string.Empty;
            }

            pages.Add(new LinkedInPageInfo(url, title, navPrimary, navGlobal, bodySample));
        }

        return pages;
    }

    private async Task<bool> IsSignedInCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            var context = await GetContextAsync();
            var page = await context.NewPageAsync();
            try
            {
                await page.GotoAsync("https://www.linkedin.com/feed/", new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = _options.ActionTimeoutMs
                });
                await page.WaitForTimeoutAsync(2000);

                var state = await GetPageStateAsync(page);
                return !IsAuthwallOrLogin(state.Url) && !state.HasLoginInput;
            }
            finally
            {
                await ClosePageAsync(page);
            }
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    private async Task<bool> SignInCoreAsync(CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        var page = await context.NewPageAsync();
        try
        {
            await page.GotoAsync("https://www.linkedin.com/login", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = _options.ActionTimeoutMs
            });

            var deadline = DateTime.UtcNow.AddMinutes(3);
            while (DateTime.UtcNow < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var state = await GetPageStateAsync(page);
                if (!IsAuthwallOrLogin(state.Url) && !state.HasLoginInput)
                {
                    return true;
                }
                await page.WaitForTimeoutAsync(1500);
            }

            return false;
        }
        finally
        {
            await ClosePageAsync(page);
        }
    }

    private async Task<LinkedInSendResult> SendInMailCoreAsync(string profileUrl, string subject, string body, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(profileUrl))
        {
            throw new ArgumentException("Lead has no LinkedIn URL.", nameof(profileUrl));
        }

        var context = await GetContextAsync();
        var page = await context.NewPageAsync();
        try
        {
            await page.GotoAsync(profileUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = _options.ActionTimeoutMs
            });

            await EnsureSignedInAsync(page);
            await page.WaitForTimeoutAsync(2000);

            var connectButton = await FindTopCardConnectAsync(page);
            if (connectButton is not null)
            {
                await ClickConnectButtonAsync(page, connectButton);
                return await SendConnectRequestAsync(page, body, cancellationToken);
            }

            var messageButton = await WaitForTextButtonAsync(page, new[] { "Message", "InMail" }, new[]
            {
                "[aria-label='Message']",
                "[aria-label*='Message']",
                "[aria-label*='InMail']"
            });

            if (messageButton is not null)
            {
                return await SendDirectMessageAsync(page, messageButton, subject, body, cancellationToken);
            }

            throw new InvalidOperationException(
                "Could not find a Message or Connect button on the profile. The lead may not be reachable, or LinkedIn changed its interface.");
        }
        finally
        {
            await ClosePageAsync(page);
        }
    }

    private async Task<LinkedInSendResult> SendDirectMessageAsync(IPage page, ILocator messageButton, string subject, string body, CancellationToken cancellationToken)
    {
        var newPageTask = page.Context.WaitForPageAsync(new BrowserContextWaitForPageOptions { Timeout = 5000 });
        await messageButton.ClickAsync(new LocatorClickOptions { Force = true });

        IPage composerPage = page;
        var openedNewTab = false;
        try
        {
            var newPage = await newPageTask;
            composerPage = newPage;
            openedNewTab = true;
            await newPage.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        }
        catch (TimeoutException)
        {
            // Composer opens in the current page as an overlay.
        }

        try
        {
            var bodyBox = await WaitForAnySelectorAsync(composerPage, new[]
            {
                "div[role='textbox']",
                ".msg-form__contenteditable"
            });
            if (bodyBox is null)
            {
                var probe = await composerPage.EvaluateAsync<dynamic>(
                    "() => ({ url: location.href, textboxes: document.querySelectorAll('[role=textbox]').length, contenteditables: document.querySelectorAll('[contenteditable]').length, hasMsgForm: !!document.querySelector('.msg-form'), bodySample: (document.body.innerText || '').slice(0, 80) })");
                throw new InvalidOperationException($"Could not open the message composer. {JsonSerializer.Serialize(probe)}");
            }

            await bodyBox.ClickAsync(new LocatorClickOptions { Force = true });
            try
            {
                await bodyBox.PressSequentiallyAsync(body, new LocatorPressSequentiallyOptions { Delay = 6 });
            }
            catch (TimeoutException)
            {
                await bodyBox.EvaluateAsync(
                    "(el, text) => { el.focus(); document.execCommand('insertText', false, text); }",
                    body);
            }

            var subjectBox = composerPage.Locator("input[name='subject'], input[placeholder*='Subject']").First;
            if (await subjectBox.CountAsync() > 0)
            {
                await subjectBox.FillAsync(subject ?? string.Empty);
            }

            if (_options.DryRun)
            {
                return new LinkedInSendResult { Sent = false, Message = "Dry run: message prepared in the composer but not sent." };
            }

            var sendButton = await WaitForAnySelectorAsync(composerPage, new[]
            {
                "button[aria-label*='Send now']",
                "button[aria-label*='Send']",
                "button.msg-form__send-button"
            });
            if (sendButton is null)
            {
                throw new InvalidOperationException("Could not find the Send button.");
            }

await sendButton.ClickAsync(new LocatorClickOptions { Force = true });
        await composerPage.WaitForTimeoutAsync(2000);

            return new LinkedInSendResult { Sent = true };
        }
        finally
        {
            if (openedNewTab)
            {
                await ClosePageAsync(composerPage);
            }
        }
    }

    private static async Task ClickConnectButtonAsync(IPage page, ILocator connectButton)
    {
        try
        {
            await connectButton.EvaluateAsync(
                "el => { el.removeAttribute('href'); el.dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true, view: window })); }");
        }
        catch (PlaywrightException ex)
        {
            throw new InvalidOperationException($"Could not click the Connect button. {ex.Message}");
        }
    }

    private async Task<LinkedInSendResult> SendConnectRequestAsync(IPage page, string body, CancellationToken cancellationToken)
    {
        var addNote = await WaitForTextButtonAsync(page, new[] { "Add a note", "Add note" }, new[]
        {
            "div[role='menu'] button[aria-label*='Add a note']"
        });
        if (addNote is null)
        {
            var probe = await page.EvaluateAsync<dynamic>(
                "() => ({ url: location.href, visibleButtons: [...document.querySelectorAll('button, a, [role=menuitem], [role=button]')].filter(e => { const r = e.getBoundingClientRect(); return r.width > 0 && r.height > 0; }).map(e => (e.innerText || '').trim().slice(0, 40)).filter(t => t).slice(0, 30) })");
            throw new InvalidOperationException($"Could not find the 'Add a note' option after clicking Connect. {JsonSerializer.Serialize(probe)}");
        }

        await addNote.ClickAsync(new LocatorClickOptions { Force = true });

        var noteBox = await WaitForAnySelectorAsync(page, new[]
        {
            "textarea[name='message']",
            "#connect-cta-form__message",
            "div[role='dialog'] textarea",
            "textarea"
        });
        if (noteBox is null)
        {
            throw new InvalidOperationException("Could not find the note field in the connect dialog.");
        }

        var noteText = body.Length <= 300 ? body : body[..300];
        await noteBox.ClickAsync(new LocatorClickOptions { Force = true });
        await noteBox.FillAsync(noteText);

        if (_options.DryRun)
        {
            return new LinkedInSendResult { Sent = false, Message = "Dry run: connect request with note prepared but not sent." };
        }

        var sendButton = await WaitForAnySelectorAsync(page, new[]
        {
            "div[role='dialog'] button[aria-label*='Send']",
            "div[role='dialog'] button:text-is('Send')",
            "button[aria-label*='Send now']",
            "button[aria-label*='Send']"
        });
        if (sendButton is null)
        {
            throw new InvalidOperationException("Could not find the Send button in the connect dialog.");
        }

        await sendButton.ClickAsync(new LocatorClickOptions { Force = true });
        await page.WaitForTimeoutAsync(2000);

        return new LinkedInSendResult { Sent = true };
    }

    private async Task<T> RunWithContextResetAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)
    {
        try
        {
            return await action();
        }
        catch (PlaywrightException ex) when (ex.Message.Contains("closed", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(ex, "LinkedIn automation browser was closed; relaunching and retrying once.");
            await ResetContextAsync();
            cancellationToken.ThrowIfCancellationRequested();
            return await action();
        }
    }

    private async Task<IBrowserContext> GetContextAsync()
    {
        if (_context is not null)
        {
            return _context;
        }

        await _gate.WaitAsync();
        try
        {
            if (_context is not null)
            {
                return _context;
            }

            try
            {
                _playwright = await Playwright.CreateAsync();
                _context = await _playwright.Chromium.LaunchPersistentContextAsync(_options.UserDataDir, new BrowserTypeLaunchPersistentContextOptions
                {
                    Headless = _options.Headless,
                    ExecutablePath = string.IsNullOrWhiteSpace(_options.BrowserExecutablePath) ? null : _options.BrowserExecutablePath,
                    Args = new[] { "--disable-blink-features=AutomationControlled" },
                    ViewportSize = new ViewportSize { Width = 1280, Height = 900 }
                });
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Could not launch the LinkedIn automation browser. Make sure the Playwright Chromium browser is installed " +
                    "(run: dotnet tool install --global Microsoft.Playwright.CLI then playwright install chromium) " +
                    $"and that LinkedIn:UserDataDir points to a valid folder. Details: {ex.Message}", ex);
            }

            return _context;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task ResetContextAsync()
    {
        await _gate.WaitAsync();
        try
        {
            if (_context is not null)
            {
                try
                {
                    await _context.DisposeAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error disposing LinkedIn browser context.");
                }
                _context = null;
            }

            _playwright?.Dispose();
            _playwright = null;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<ILocator?> FindTopCardConnectAsync(IPage page)
    {
        string name;
        try
        {
            name = await page.EvaluateAsync<string>("() => { const t = document.title || ''; const i = t.indexOf(' | '); return i > 0 ? t.slice(0, i) : ''; }");
        }
        catch
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return await FirstVisibleWithWaitAsync(page.Locator($"a[aria-label*='{name}' i][aria-label*='connect' i], button[aria-label*='{name}' i][aria-label*='connect' i]"), 8000);
    }

    private static async Task<ILocator?> FirstVisibleWithWaitAsync(ILocator locator, int timeoutMs)
    {
        var deadline = Environment.TickCount64 + timeoutMs;
        while (Environment.TickCount64 < deadline)
        {
            var count = await locator.CountAsync();
            for (var i = 0; i < count; i++)
            {
                var nth = locator.Nth(i);
                if (await nth.IsVisibleAsync())
                {
                    return nth;
                }
            }

            await Task.Delay(300);
        }

        return null;
    }

    private async Task EnsureSignedInAsync(IPage page)
    {
        await page.WaitForTimeoutAsync(2000);
        var state = await GetPageStateAsync(page);
        if (IsAuthwallOrLogin(state.Url) || state.HasLoginInput)
        {
            throw new InvalidOperationException(
                "Not signed in to LinkedIn. Launch the app, log into LinkedIn once in the automation browser window, then try again.");
        }
    }

    private static async Task<(string Url, bool HasLoginInput)> GetPageStateAsync(IPage page)
    {
        var json = await page.EvaluateAsync<JsonElement>(
            "() => ({ url: location.href, hasLoginInput: !!document.querySelector('#username') })");
        return (json.GetProperty("url").GetString() ?? string.Empty, json.GetProperty("hasLoginInput").GetBoolean());
    }

    private static bool IsAuthwallOrLogin(string url)
    {
        return url.Contains("/login", StringComparison.OrdinalIgnoreCase)
            || url.Contains("/authwall", StringComparison.OrdinalIgnoreCase)
            || url.Contains("/checkpoint", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<ILocator?> WaitForAnySelectorAsync(IPage page, IReadOnlyList<string> selectors)
    {
        foreach (var selector in selectors)
        {
            var locator = page.Locator(selector).First;
            try
            {
                await locator.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = 8000
                });
                return locator;
            }
            catch (TimeoutException)
            {
                // Try the next selector.
            }
        }

        return null;
    }

    private static async Task<ILocator?> WaitForTextButtonAsync(IPage page, IReadOnlyList<string> texts, IReadOnlyList<string> cssFallbacks)
    {
        foreach (var selector in cssFallbacks)
        {
            var locator = await FirstVisibleWithWaitAsync(page.Locator(selector), 8000);
            if (locator is not null)
            {
                return locator;
            }
        }

        foreach (var text in texts)
        {
            var locator = await FirstVisibleWithWaitAsync(page.GetByText(text, new PageGetByTextOptions { Exact = true }), 8000);
            if (locator is not null)
            {
                return locator;
            }
        }

        return null;
    }

    private static async Task ClosePageAsync(IPage page)
    {
        try
        {
            await page.CloseAsync();
        }
        catch (Exception ex)
        {
            // The browser may already be gone; nothing to do.
            _ = ex;
        }
    }
}