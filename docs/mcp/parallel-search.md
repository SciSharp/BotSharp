# Parallel Search MCP

BotSharp can use the free, keyless [Parallel Search MCP](https://docs.parallel.ai/integrations/mcp/search-mcp) service for web search and page extraction. This example uses the existing HTTP MCP integration. It does not require a Parallel API key or a new plugin.

## Configure a server

The [example configuration](parallel-search.json) uses BotSharp's `MCP` section, `HttpConfig`, and the Streamable HTTP endpoint `https://search.parallel.ai/mcp`. Add its server object to `MCP.McpServerConfigs` in your host's configuration. Keep any existing server entries and client settings. The example's `User-Agent` identifies the caller as BotSharp; update its version when using another BotSharp release.

For a host that registers MCP directly, load your configuration and call:

```csharp
services.AddBotSharpMCP(configuration);
```

WebStarter already calls `AddBotSharpMCP` in `Program.cs`. Keep `MCP.Enabled` and this server's `Enabled` set to `true` to use it. Setting the server's `Enabled` to `false` prevents connections to that server. The repository's WebStarter configuration and existing provider selections are unchanged by this example.

## Select tools for an agent

Use the MCP server list in BotSharp's UI to select **Parallel Search**, then enable `web_search` and `web_fetch` for the agent. The server list discovers the tools through `IMcpService.GetServerConfigsAsync()`. The corresponding agent `mcp_tools` entry is:

```json
{
  "name": "Parallel Search",
  "server_id": "parallel-search",
  "disabled": false,
  "functions": [
    { "name": "web_search" },
    { "name": "web_fetch" }
  ]
}
```

BotSharp's MCP agent hook adds the selected tools to the agent's secondary functions during a conversation. Its executor factory routes calls to `McpToolExecutor`, which returns the server's text content to the conversation.

Ask the agent to find Microsoft's official introduction to .NET and explain what .NET is using that source. `web_search` takes an `objective` and a `search_queries` array. Its excerpts can often answer the question directly. Use `web_fetch`, with a `urls` array and optional `objective`, when a specific page or exact wording is needed. Both tools also support a shared `session_id` for the conversation. Your agent still needs its existing LLM configuration; keyless search does not supply an LLM.

## Run the example validation

With the .NET 8 SDK, from the repository root:

```sh
dotnet test tests/BotSharp.Core.UnitTests/BotSharp.Core.UnitTests.csproj \
  --filter FullyQualifiedName~ParallelSearchExampleTests \
  --logger "console;verbosity=detailed"
```

By default this runs a local HTTP fixture with no external requests. To use the public service explicitly, on a POSIX shell:

```sh
BOTSHARP_PARALLEL_LIVE=1 dotnet test tests/BotSharp.Core.UnitTests/BotSharp.Core.UnitTests.csproj \
  --filter FullyQualifiedName~ParallelSearchExampleTests \
  --logger "console;verbosity=detailed"
```

For PowerShell, set `$env:BOTSHARP_PARALLEL_LIVE = "1"` before the same test command, then remove it with `Remove-Item Env:BOTSHARP_PARALLEL_LIVE` when done. Live validation requires network access and is subject to the service's availability and usage limits.

The test loads the example JSON through `AddBotSharpMCP`, discovers tools through `IMcpService`, loads them through the agent hook, and invokes search and fetch through the executor factory. It prints the returned source content and checks that discovery and tool calls identify BotSharp without authentication headers. It uses test conversation state and no LLM, saved settings, environment configuration provider, or credential store. It also checks that disabling the agent's MCP entry removes the tools and disabling the server prevents a connection.
