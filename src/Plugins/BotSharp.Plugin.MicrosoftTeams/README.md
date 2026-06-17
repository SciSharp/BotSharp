# BotSharp.Plugin.MicrosoftTeams

Two-way Microsoft Teams integration for BotSharp, built on **Azure Bot Service / Bot Framework SDK**.

- **Inbound** (user → bot): Teams activities arrive at the messaging endpoint and are routed into the BotSharp conversation engine.
- **Outbound / proactive** (bot → user): the bot pushes unsolicited messages back to users it has previously talked to, via stored conversation references.

## Endpoints

| Method | Route | Auth | Purpose |
|--------|-------|------|---------|
| POST | `/teams/messages/{agentId}` | Bot Framework JWT | Azure Bot "messaging endpoint" (inbound) |
| POST | `/teams/notify` | Platform `[Authorize]` | Proactive push (outbound) |

`/teams/notify` body:

```jsonc
// send literal text
{ "userId": "<aadObjectId>", "text": "Your ticket #123 was resolved." }

// or let an agent generate the reply
{ "userId": "<aadObjectId>", "agentId": "<agentId>", "prompt": "Summarize ticket #123 status" }
```

## Configuration

`appsettings.json`:

```jsonc
"MicrosoftTeams": {
  "AppType": "MultiTenant",   // MultiTenant | SingleTenant | UserAssignedMSI
  "AppId": "",                // Azure Bot app (client) id
  "AppPassword": "",          // client secret — use env var / Key Vault, never commit
  "TenantId": "",             // required for SingleTenant / UserAssignedMSI
  "AgentId": ""               // default agent for /teams/notify when prompt is used
}
```

> **Security:** `AppPassword` is a client secret — keep it out of source control (User Secrets / Key Vault / env var). For production prefer `UserAssignedMSI` (managed identity, no secret). The inbound action is `[AllowAnonymous]` only because request legitimacy is enforced by the Bot Framework JWT pipeline inside the adapter — that layer must stay enabled.

## Setup

1. **Azure Bot resource** — Azure Portal → create an *Azure Bot* → note the App ID and create a client secret. Under *Channels*, add **Microsoft Teams**. Set *Messaging endpoint* to `https://<host>/teams/messages/{agentId}` (substitute a real agent id).
2. **Teams app package** — author `manifest.json` (see `manifest/` below), zip it with `color.png` (192×192) and `outline.png` (32×32), and side-load via the Teams *Developer Portal* or *Apps → Manage your apps → Upload a custom app*.
3. **Register the plugin** — already added to `WebStarter/appsettings.json` `PluginLoader.Assemblies`. Fill in the `MicrosoftTeams` settings.
4. **Test** — use the *Bot Framework Emulator* for local turn logic, then `devtunnel`/`ngrok` to expose the endpoint for real Teams side-loading.

## Rich content mapping

`AdaptiveCardConverter` maps BotSharp rich messages to Teams:

| BotSharp | Teams |
|----------|-------|
| `TextMessage` / plain content | text activity |
| `QuickReplyMessage` | Adaptive Card with `Action.Submit` buttons (payload preserved) |
| `ButtonTemplateMessage` | Adaptive Card: `web_url` → `Action.OpenUrl`, else `Action.Submit` |

Submit payloads come back in `Activity.Value.payload`, which the bot unwraps into the next user turn.

## Notes

- The conversation-reference store defaults to in-memory (single node). For multi-instance deployments, implement `IConversationReferenceStore` against a durable store (Mongo/Redis/BotSharp storage) and register it in place of `InMemoryConversationReferenceStore`.
- The Bot Framework SDK is in long-term maintenance; the successor is the **Microsoft 365 Agents SDK**. The integration seam here (adapter + `IBot` + `ActivityHandler`) maps cleanly onto it if you migrate later.
