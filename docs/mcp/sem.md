# Local code lookup with Sem

BotSharp's stdio MCP integration can start [Sem](https://github.com/Ataraxy-Labs/sem)
to look up source code in a local checkout. This optional configuration does not
change WebStarter defaults or require another BotSharp plugin.

## Configure the server

Install Sem on the machine or container running BotSharp (`sem --version`;
tested with 0.27.0). Add the server object from [sem.json](sem.json) to your
existing `MCP.McpServerConfigs` array, preserving other servers and settings.
Replace `SEM_REPO` with an absolute checkout path visible to that process.
If `sem` is not on the host's `PATH`, use its absolute executable path in
`StdioConfig.Command`. Keep `MCP.Enabled` and the server's `Enabled` set to `true`.

WebStarter already registers `AddBotSharpMCP(configuration)`. Hosts that register
services themselves need the same call.

## Select code lookup tools

In the MCP server list, select **Sem local code** and enable `sem_find` and
`sem_grep` for the agent. The equivalent agent `mcp_tools` entry is:

```json
{
  "name": "Sem local code",
  "server_id": "sem",
  "disabled": false,
  "functions": [{ "name": "sem_find" }, { "name": "sem_grep" }]
}
```

Ask the agent to find a known symbol in the selected checkout. For example,
`sem_find` accepts `{"query":"parse_config","mode":"context","token_budget":1200}`.
BotSharp routes selected functions through its existing MCP executor. The agent
still uses its configured LLM; Sem does not supply a model or model credentials.

Sem may write a local index. `SEM_REPO` is a repository selector, not an access
control boundary. Run only against trusted checkouts under an appropriately
restricted OS account. The full server also exposes validation tools that can
execute commands, so retain the explicit tool selection. Source returned by
tools will be sent to the agent's configured model.

Disable the server with `Enabled: false`, or remove this server entry and the
agent's corresponding `mcp_tools` entry. No existing provider settings need to
change.

## Validate the example

From the repository root with the .NET 8 SDK:

```sh
dotnet test tests/BotSharp.Core.UnitTests/BotSharp.Core.UnitTests.csproj --filter FullyQualifiedName~SemExampleTests
```

The configuration test does not launch Sem. To also exercise discovery and
`sem_find` through BotSharp's client manager, set `BOTSHARP_SEM_TEST_REPO` to an
absolute disposable checkout containing a `parse_config` symbol before running
the same command. The live test is opt-in; it requires Sem on `PATH` but no LLM.
