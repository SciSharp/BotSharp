using System.Text.Json;
using BotSharp.Core.MCP.Managers;
using ModelContextProtocol.Client;

namespace BotSharp.Core.MCP.Functions;

public class McpToolAdapter : IFunctionCallback
{
    private readonly string _provider;
    private readonly McpClientTool _tool;
    private readonly McpClientManager _clientManager;
    private readonly IServiceProvider _services;

    public McpToolAdapter(
        IServiceProvider services,
        string serverName,
        McpClientTool tool,
        McpClientManager client)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _tool = tool ?? throw new ArgumentNullException(nameof(tool));
        _clientManager = client ?? throw new ArgumentNullException(nameof(client));
        _provider = serverName;
    }

    public string Provider => _provider;
    public string Name => _tool.Name;

    public async Task<bool> Execute(RoleDialogModel message)
    {
        // Convert arguments to dictionary format expected by mcpdotnet
        Dictionary<string, object> argDict = JsonToDictionary(message.FunctionArgs);
        var currentAgentId = message.CurrentAgentId;
        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.LoadAgent(currentAgentId);
        var serverId = agent.McpTools.Where(t => t.Functions.Any(f => f.Name == Name)).FirstOrDefault().ServerId;

        var client =  await _clientManager.GetMcpClientAsync(serverId);

        // Call the tool through mcpdotnet
        var result = await client.CallToolAsync(_tool.Name, argDict.IsNullOrEmpty() ? new() : argDict);

        // Extract the text content from the result
        var json = string.Join("\n", result.Content.Where(c => c.Type == "text").Select(c => c.Text));

        message.Content = json;
        message.Data = json.JsonContent();
        return true;
    }

    private static Dictionary<string, object> JsonToDictionary(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return [];

        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;
        return JsonElementToDictionary(root);
    }

    private static Dictionary<string, object> JsonElementToDictionary(JsonElement element)
    {
        Dictionary<string, object> dictionary = [];

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in element.EnumerateObject())
            {
                dictionary[property.Name] = JsonElementToValue(property.Value);
            }
        }

        return dictionary;
    }

    private static object? JsonElementToValue(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => JsonElementToDictionary(element),
        JsonValueKind.Array => element.EnumerateArray().Select(JsonElementToValue).ToList(),
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number when element.TryGetInt32(out int intValue) => intValue,
        JsonValueKind.Number when element.TryGetInt64(out long longValue) => longValue,
        JsonValueKind.Number when element.TryGetDouble(out double doubleValue) => doubleValue,
        JsonValueKind.Number when element.TryGetDecimal(out decimal decimalValue) => decimalValue,
        JsonValueKind.Number when element.TryGetByte(out byte byteValue) => byteValue,
        JsonValueKind.Number when element.TryGetSByte(out sbyte sbyteValue) => sbyteValue,
        JsonValueKind.Number when element.TryGetUInt16(out ushort uint16Value) => uint16Value,
        JsonValueKind.Number when element.TryGetUInt32(out uint uint32Value) => uint32Value,
        JsonValueKind.Number when element.TryGetUInt64(out ulong uint64Value) => uint64Value,
        JsonValueKind.Number when element.TryGetDateTime(out DateTime dateTimeValue) => dateTimeValue,
        JsonValueKind.Number when element.TryGetDateTimeOffset(out DateTimeOffset dateTimeOffsetValue) => dateTimeOffsetValue,
        JsonValueKind.Number when element.TryGetGuid(out Guid guidValue) => guidValue,
        JsonValueKind.Number => element.GetRawText(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        JsonValueKind.Undefined => string.Empty, // JsonElement is undefined (there is no value).
        _ => throw new ArgumentOutOfRangeException(nameof(element.ValueKind), element.ValueKind, "Unexpected JsonValueKind encountered.")
    };

}
