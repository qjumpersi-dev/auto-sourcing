# ScottyAI Integration — Developer Guide

How the AI Talent Sourcing app (Auto Sourcing / `quickmatchai.com`) integrates with the **ScottyAI** agent platform for **text chat** and **voice**, including the architecture, the exact APIs, where credentials live, how to rebuild it from scratch, and every issue we hit (with fixes).

> Audience: developers maintaining or replicating this integration.

---

## 1. What this integration does

We host two "personas" of an AI assistant on ScottyAI and surface them in the app:

| Persona | Who talks to it | Where it appears | Agent on Scotty |
|---|---|---|---|
| **Recruiter / user assistant** | QJumpers recruiters (internal) | Floating chat bubble + voice button over the whole app (`ScottyAssistant`) | `AITS Helper Agent` |
| **Candidate helper** | Candidates (via a link per lead) | Server-rendered page at `/api/agent/{leadId}` with chat + "Talk" button | `Candidate Job Helper` |

Each persona has **two channels**: a REST channel (text) and a WebRTC channel (voice). ScottyAI's **Dispatcher** routes each request through a **Routing Rule** → **Pipeline** → **Agent**.

```
Browser ──► App backend (.NET) ──► ScottyAI API ──► Dispatcher ──► Pipeline ──► Agent
                  │                                                 (REST /    (prompt, model,
                  │                                                  Live)      memory, MCP tools)
                  └─ holds the Scotty API key server-side
```

- **Text**: request/response. App backend calls Scotty's REST channel endpoint, gets the agent's reply, returns it to the browser.
- **Voice**: app backend asks Scotty for a short-lived **LiveKit** join credential (`url` + `token`), returns that to the browser, and the browser connects to the LiveKit room directly for audio. The API key never reaches the browser.

---

## 2. ScottyAI resources (current production IDs)

Project: **AITS Help Agent**

| Resource | Name | UUID |
|---|---|---|
| Organization | — | `d90ddcff-a775-46e8-ba0b-edb5d8d3bb17` |
| Project | AITS Help Agent | `9d16e619-7b31-40cb-875a-b8fc5a460464` |
| Folder | Simon's Stuff | `747bf0d5-6e1f-46a9-9955-1338f36f416a` |
| Agent (recruiter) | AITS Helper Agent | `1164ddaf-56ef-42a1-b40a-ccb7ea525979` |
| Agent (candidate) | Candidate Job Helper | `8a457499-a008-44c3-8270-07423315b92c` |
| Pipeline (text) | AITS Chatbot (`messaging_v1`) | `a14c7f7f-3bf2-49f5-b5d4-9463a808ce28` |
| Pipeline (voice) | AITS Voice Helper (`live_v0`) | `d836870e-eb60-4b3f-96da-79324be4f1e9` |
| Channel (text, recruiter) | AITS Helper Chatbot (`messaging/rest`) | `87cbd107-6920-40fc-9965-af05c9c68b78` |
| Channel (voice, recruiter) | AITS Helper Voice Chat (`live/webrtc`) | `6f9b1325-6066-4ea4-897a-41d1fc2e3381` |
| Channel (text, candidate) | AITS Candidate Chat (`messaging/rest`) | `d47aa357-ea09-4015-aa2a-5d84ef242d2d` |
| Channel (voice, candidate) | AITS Candidate Voice (`live/webrtc`) | `50337a69-ddf3-4fc0-9de9-c801532c54f1` |
| MCP server | AITS Recruitment Tools | `4589304c-177c-4474-ba77-32a9b55b5832` |
| MCP integration | Candidate Helper MCP (attached to Candidate agent) | `fe12b7ae-0ef4-4340-9ee0-ca96f4608234` |

Routing rules (see §7 for the critical `continuity_key` detail):

| Rule | Channel | Agent | Pipeline |
|---|---|---|---|
| Default REST Route | `87cbd107…` | AITS Helper Agent | AITS Chatbot |
| Candidate REST Route | `d47aa357…` | Candidate Job Helper | AITS Chatbot |
| Default Voice Route | `6f9b1325…` | AITS Helper Agent | AITS Voice Helper |
| Candidate Voice Route | `50337a69…` | Candidate Job Helper | AITS Voice Helper |

---

## 3. ScottyAI API

- **Base URL (current):** `https://api.eu1.scotty-ai.com/v1`
  - ⚠️ Was `https://api.scotty-ai.com/v1`. ScottyAI is retiring location-less addresses (deadline **16 Nov 2026**). Always use the `eu1` (location) address going forward. Web console is `https://app.eu1.scotty-ai.com`.
- **Auth header:** `API-KEY: <org API key>` + `Content-Type: application/json`
  - The **REST channel** endpoint is technically public (no auth), but we send the key anyway. The **WebRTC** endpoint requires the key.
- **JSON casing:** request bodies use snake_case (`user_prompt`, `continuity_key`, `session_participant_id`); responses mix camelCase top-level (`output`, `attachments`, `metadata`, `url`, `token`) and snake_case inside `metadata` (`platform_session_id`, `agent_definition_id`, …). Parse defensively.

### 3.1 Text (REST channel)

```
POST {base}/channels/rest/{channel_definition_id}
Content-Type: application/json

{ "user_prompt": "Hello", "continuity_key": "<uuid>" }
```

Response (200):

```json
{
  "output": "Agent reply text",
  "attachments": [],
  "metadata": {
    "platform_session_id": "…",
    "continuity_key": "…",
    "agent_instance_id": "…",
    "agent_definition_id": "…",
    "pipeline_instance_id": "…"
  }
}
```

Notes:
- `continuity_key` **must be a UUID**. Same UUID = same conversation (memory persists); new UUID = new conversation.
- `user_prompt` can be a string, or an array for multimodal (`[{ "content_type": "image/png", "binary_data": "<base64>" }]`).
- Errors: `404` channel not found/inactive, `422` validation (missing/invalid fields), `429` rate limit, `500`.

### 3.2 Voice (WebRTC / LiveKit)

```
POST {base}/channels/webrtc/{channel_definition_id}
API-KEY: <org API key>
Content-Type: application/json

{ "session_participant_id": "<uuid>", "continuity_key": "<uuid>" }
```

Response (200):

```json
{ "url": "wss://scotty-ai-prd-….livekit.cloud", "token": "eyJ…", "metadata": { … } }
```

The browser uses `url` + `token` with the **LiveKit** JS SDK to join the room, publish the mic, and play the agent's audio. The API key is never exposed to the browser.

### 3.3 Admin/definition endpoints (used during setup)

All under `/v1/projects/{project_id}/…`:

- `GET/PUT channel-definitions`
- `GET/PUT pipeline-definitions`
- `GET/PUT agent-definitions` (GET list & detail; see §10 warning about PUT)
- `GET/PUT/DELETE routing-rules`
- `GET mcp-servers`, `GET mcp-integrations`
- `GET projects`

---

## 4. Application code map

### Backend (.NET 8)

- `src/AutoSourcing.Services/Scotty/`
  - `ScottyOptions.cs` — `BaseUrl`, `ApiKey`, `RestChannelId`, `WebRtcChannelId`, `CandidateRestChannelId`, `CandidateWebRtcChannelId`.
  - `IScottyClient.cs` / `ScottyClient.cs`
    - `SendTextAsync(ScottyChatRequest)` — POST to `/channels/rest/{ChannelId}` (channel can be overridden per request via `ChannelId`).
    - `GetCallCredentialAsync(ScottyCallRequest)` — POST to `/channels/webrtc/{ChannelId}`.
    - Builds the full absolute URI from `BaseUrl` (see issue #4 about `HttpClient` + leading slash).
- `src/AutoSourcing.API/Program.cs` — `Configure<ScottyOptions>(…)` + `AddHttpClient<IScottyClient, ScottyClient>()`.
- `src/AutoSourcing.API/Controllers/`
  - `ScottyController.cs` — recruiter widget: `POST /api/scotty/chat`, `POST /api/scotty/call` (uses the recruiter channels).
  - `CandidateAgentController.cs` — candidate page + APIs:
    - `GET /api/agent/{leadId}` — server-rendered HTML chat page (with chat + "Talk" button, loads LiveKit UMD from CDN).
    - `POST /api/agent/{leadId}/chat` — text; uses `lead.ConversationKey` as the `continuity_key` and the **candidate** REST channel.
    - `POST /api/agent/{leadId}/call` — voice; returns LiveKit `url`/`token` using the **candidate** WebRTC channel.
- `src/AutoSourcing.API/appsettings.json` — `Scotty` section (BaseUrl + channel IDs; `ApiKey` left blank, injected via env).

### Frontend (React + TypeScript + Vite)

- `client/src/components/ScottyAssistant.tsx` — floating chat bubble + voice button.
  - Text via `useScottyChatMutation`; voice via `useScottyCallMutation` + `livekit-client`.
  - Agent replies rendered as **Markdown** (`react-markdown` + `remark-gfm`; raw HTML not rendered).
  - `continuity_key` stored per browser in `localStorage`.
- `client/src/services/apiSlice.ts` — `scottyChat`, `scottyCall` mutations.
- `client/src/types/models.ts` — `ScottyChatResponse`, `ScottyCallResponse`, `ScottyMetadata`, `ScottyAttachment`.
- `client/src/App.tsx` — mounts `<ScottyAssistant />` globally.
- The **candidate** page is not React — it's HTML generated in `CandidateAgentController`. It loads `livekit-client` from CDN (`https://cdn.jsdelivr.net/npm/livekit-client@2.22.1/dist/livekit-client.umd.js`, global `LivekitClient`).

---

## 5. Configuration & secrets (where the keys are, and reuse)

### Where secrets live
- **Scotty org API key**: stored **server-side only** in the droplet's `/root/auto-sourcing/.env` as `SCOTTY_API_KEY`. `docker-compose.prod.yml` injects it into the API container as `Scotty__ApiKey`. It is **not** in git (appsettings has an empty `ApiKey`).
  - Locally, use `dotnet user-secrets` or an env var `Scotty__ApiKey` — never commit it.
- **MCP `X-API-Key`** (the app's own MCP auth, not a Scotty key): stored in the Scotty **MCP server / MCP integration** config (`AITS Recruitment Tools` / `Candidate Helper MCP` headers). It authenticates Scotty → `https://www.quickmatchai.com/api/mcp`.
- **Channel / agent / pipeline / routing UUIDs**: not secret. Configured in `appsettings.json` (per environment) or env vars.

### Can the dev team reuse the existing keys?
Yes.
- The **Scotty API key is organisation-scoped** — the same key works for any client/script against the same org/project. New environments can reuse it, or Scotty can issue additional keys under **Admin → API Key Manager** if you want separate keys per environment/service (recommended for production separation).
- The **MCP `X-API-Key`** is the app's own key and is reusable for any Scotty agent that needs to call `quickmatchai.com/api/mcp`.
- The **channel IDs differ per persona** (recruiter vs candidate). If you build a new environment/project, create new channels and put their IDs in config — don't hardcode.

### `.env` keys used by the droplet (`docker-compose.prod.yml`)
```
SCOTTY_API_KEY=…
SCOTTY_REST_CHANNEL_ID=87cbd107-…        # recruiter text
SCOTTY_WEBRTC_CHANNEL_ID=6f9b1325-…      # recruiter voice
SCOTTY_CANDIDATE_REST_CHANNEL_ID=d47aa357-…   # candidate text
SCOTTY_CANDIDATE_WEBRTC_CHANNEL_ID=50337a69-… # candidate voice
```
These map to `Scotty__ApiKey`, `Scotty__RestChannelId`, `Scotty__WebRtcChannelId`, `Scotty__CandidateRestChannelId`, `Scotty__CandidateWebRtcChannelId`.

---

## 6. Build it from scratch (checklist)

1. **Agents** (Scotty UI → Agent Definitions): create one per persona.
   - Set prompt, model (chat kind), memory (long TTL), and (candidate) MCP tools.
   - Recruiter agent: internal help prompt. Candidate agent: candidate-facing prompt that grounds via tools.
2. **Pipelines** (Connectivity → Pipelines):
   - Messaging pipeline (`messaging_v1`) for text.
   - **Live pipeline (`live_v0`) with STT + TTS + turn detection configured** — required for voice (see issue #2). We use *Deepgram STT Via Scotty* + *Google TTS Via Scotty*.
3. **Channels** (Connectivity → Channel Definitions): for **each persona**, create a `messaging/rest` channel and a `live/webrtc` channel. Note the channel UUIDs.
4. **Routing Rules** (Connectivity → Routing Rules): one per channel → correct agent + pipeline.
   - **Set the continuity key correctly** (see issue #1): for a **REST** channel use **`session_id`**. (Simple mode sets this for you; in Advanced mode pick `session_id`.)
5. **MCP tools** (candidate agent): attach the `AITS Recruitment Tools` MCP server (URL `https://www.quickmatchai.com/api/mcp`, `X-API-Key` header, allow all tools).
6. **App config**: put the channel IDs + base URL + API key into `appsettings`/env (server-side).
7. **Test** (see §9).

---

## 7. Issues we hit and how we fixed them

### #1 — Continuity broken: new session every message (the big one)
- **Symptom:** The agent never remembered the previous turn. Two calls with the same `continuity_key` returned **different** `platform_session_id`s ("My name is Simon" → "What is my name?" → "I don't have your name").
- **Cause:** Routing rules had `continuity_key: "user_id"`. A routing rule's continuity key names the **session field** the dispatcher looks up. On a **REST** channel the request's `continuity_key` is stored in a field called **`session_id`** — `user_id` is never populated, so nothing links the conversation.
- **Fix:** Set the **REST** routing rules' Continuity Key to **`session_id`** (Simple mode does this automatically). Verified: same key → same session, memory works. Memory TTL was irrelevant.
- **Status:** REST rules fixed. **Voice (WebRTC) rules still `user_id`** — confirm the correct key for the live channel with Scotty before relying on voice continuity.

### #2 — Voice returns 404/502 / fails
- **Symptom:** `POST /channels/webrtc/{id}` failed; no LiveKit credential.
- **Cause:** A **chat** model paired with a **Live pipeline that had no STT/TTS**. Scotty's API masked this as a generic error (and initially returned 404 for the un-routed channel).
- **Fix:** Configure **STT + TTS** (and turn detection/VAD) on the live pipeline, and ensure the channel has a routing rule to that pipeline + agent. Then the endpoint returns `url` + `token`.

### #3 — Docs endpoints don't match the live API
- Scotty docs used `/projects/{id}/channels`; the live API uses **`/channel-definitions`**, `/pipeline-definitions`, `/agent-definitions`, `/routing-rules`. The WebRTC call path is `/channels/webrtc/{channel_definition_id}` (docs had a placeholder mistake like `/channels/rest/message`). Verify against the live API, not just docs.

### #4 — .NET `HttpClient` dropped `/v1`
- **Symptom:** Backend got 404 while the same URL worked in the console.
- **Cause:** `BaseAddress = "…/v1"` + a request path starting with `/` resets to the host, dropping `/v1`.
- **Fix:** Build an absolute URI: `new Uri($"{BaseUrl.TrimEnd('/')}/channels/rest/{id}")` (see `ScottyClient`).

### #5 — `continuity_key` must be a UUID
- `422 … continuity_key: Input should be a valid UUID`. Use a real UUID; don't send names like `test-user-1`. The candidate page uses `Lead.ConversationKey`; the widget uses a per-browser UUID.

### #6 — MCP tools detached from the agent
- **Symptom:** Agent answered "I don't have the company information" — it had no tools.
- **Cause:** The agent's `mcp_integrations` became empty (dropped during a UI re-save). The UI *showed* the MCP chip, but the saved payload (`mcp_integrations`) was `null`.
- **Fix:** In **Edit Agent Definition**, re-select **Prompt, Model, Memory, and the MCP integration**, then **Submit**. Verify via API that `resources.mcp_integrations` is populated and the server URL is correct.

### #7 — ⚠️ The Agent Definitions `PUT` API does not apply `resources`
- `PUT /v1/projects/{id}/agent-definitions/{uuid}` accepts a `resources` field but **does not apply it** — we tried embedded objects, UUID strings, `{uuid}` objects, and top-level fields; all left the agent with empty resources (and it **wiped** prompt/model/memory once). There is no `/resources` sub-endpoint and no OpenAPI spec.
- **Guidance:** Manage agent resources through the **UI** (prompt/model/memory/MCP). Do **not** use the API PUT to set resources until Scotty documents the correct schema. (We have flagged this to Scotty.)

### #8 — One agent/channel used for two personas
- **Symptom:** Candidates got the recruiter persona.
- **Cause:** A single REST channel was used for both the recruiter assistant and the candidate agent.
- **Fix:** Separate channels per persona, each with a routing rule pointing to the correct agent. The candidate page must use `CandidateRestChannelId`, not `RestChannelId`.

### #9 — Prompt "wrong variable format" warnings
- The recruiter prompt documents email personalisation tokens (`{{FirstName}}`, `{{Company}}`, `{{OrgName}}`, …). Scotty flags any `{{…}}` it can't map to a project variable. These are **intentional documentation**, not substitutions → safe to ignore. Only add real Variables (referenced by UUID) if you actually need interpolation.

### #10 — Candidate tools need continuity (baggage) beyond REST
- The candidate MCP tools (`get_candidate_context`, `get_company_info`, …) rely on the **continuity key** being forwarded to the MCP server (via the W3C `baggage` header). Fixing #1 is a prerequisite; verify tool calls actually receive the key when testing candidate context.

### #11 — Locations migration (domain change)
- `api.scotty-ai.com` → **`api.eu1.scotty-ai.com`**; web console `app.scotty-ai.com` → **`app.eu1.scotty-ai.com`** (from 16 Nov 2026, no redirect). Update all base URLs, bookmarks, and any provider webhooks (WhatsApp/SIP) hosts. The chat widget already uses `chat.eu1…`.

### #12 — Droplet `git pull` silently did nothing
- **Symptom:** Deploy rebuilt from old code (all Docker layers "CACHED"); pull warned *"…merge with ref 'refs/heads/edit/candidate-location-map' … no such ref was fetched."*
- **Cause:** The droplet was checked out on a **deleted** branch, so `git pull` couldn't merge.
- **Fix:** `git -C /root/auto-sourcing checkout -B <deploy-branch> origin/<deploy-branch>` then `git pull` + rebuild. Confirm a rebuild actually happened by checking `COPY . .` is **not** "CACHED".

---

## 8. Verification / testing

**Text (direct):** POST `/channels/rest/{channel}` twice with the same `continuity_key`:
- Turn 1: `"My name is Simon."`
- Turn 2: `"What is my name?"`
- Expect: **same `platform_session_id`** and the reply knows the name.

**Voice:** POST `/channels/webrtc/{channel}` → expect `url` + `token` (with `agent_instance_id` in metadata). In the browser, click **Talk**, allow the mic, speak, and hear the agent.

**Through the app:**
- Recruiter widget: chat bubble (Markdown-rendered) + green phone button.
- Candidate page: `GET /api/agent/{leadId}` then `POST /api/agent/{leadId}/chat`.

---

## 9. Security

- **Never** expose the Scotty API key in the browser. All Scotty calls happen in the .NET backend; the browser only receives a short-lived LiveKit `url`/`token`.
- **Derive `continuity_key` from the authenticated session** in production (e.g., the lead/user identity), not from untrusted client input. The candidate page uses `Lead.ConversationKey`; the widget currently uses a per-browser UUID (acceptable for the internal tool).
- The **REST channel is public** (no auth), so anyone with the channel UUID can post to it. If that's a concern, front it with an app endpoint that rate-limits and authorises (which is what the app does for the candidate page).
- Keep all secrets in `.env`/user-secrets, never in git.

---

## 10. Deployment

- DigitalOcean droplet, **Docker Compose** (`docker-compose.prod.yml`): `caddy` (reverse proxy/TLS), `web` (nginx, built React app), `api` (.NET), `sqlserver`.
- Deploy:
  ```
  git -C /root/auto-sourcing pull
  cd /root/auto-sourcing && sudo docker compose -f docker-compose.prod.yml up -d --build api   # or `web`
  ```
  Rebuild `api` for backend/appsettings changes, `web` for frontend changes.
- Secrets come from `/root/auto-sourcing/.env`.

---

## 11. Open items / follow-ups

- **Voice continuity key**: confirm with Scotty which continuity key a **WebRTC** channel rule should use (currently `user_id`), and update the two voice rules if needed.
- **Agent Definitions API**: Scotty to document the correct `resources` payload (or provide a resources endpoint) so agent provisioning can be automated safely.
- **Domain migration**: finish bookmarks; update WhatsApp/SIP provider hosts if/when used (SIP not currently used — voice is WebRTC).
