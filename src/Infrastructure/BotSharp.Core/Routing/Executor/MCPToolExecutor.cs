using BotSharp.Abstraction.Routing.Executor;
using BotSharp.Core.MCP.Managers;
using ModelContextProtocol.Client;

namespace BotSharp.Core.Routing.Executor;

public class McpToolExecutor: IFunctionExecutor
{
    private readonly IServiceProvider _services;
    private readonly string _mcpServerId;
    private readonly string _functionName;

    public McpToolExecutor(IServiceProvider services, string mcpServerId, string functionName)
    { 
        _services = services;
        _mcpServerId = mcpServerId;
        _functionName = functionName;
    }

    public async Task<bool> ExecuteAsync(RoleDialogModel message)
    {
        try
        {
            // Convert arguments to dictionary format expected by mcpdotnet
            Dictionary<string, object> argDict = JsonToDictionary(message.FunctionArgs);

            var clientManager = _services.GetRequiredService<McpClientManager>();
            var client = await clientManager.GetMcpClientAsync(_mcpServerId);

            // Call the tool through mcpdotnet
            var result = await client.CallToolAsync(_functionName, !argDict.IsNullOrEmpty() ? argDict : []);

            // Extract the text content from the result
            var json = string.Join("\n", result.Content.Where(c => c.Type == "text").Select(c => c.Text));

            message.Content = json;
            message.Data = json.JsonContent();
            return true;
        }
        catch (Exception ex)
        {
            message.Content = $"Error when calling tool {_functionName} of MCP server {_mcpServerId}. {ex.Message}";
            return false;
        }
    }

    public async Task<string> GetIndicatorAsync(RoleDialogModel message)
    {
        return message.Indication ?? string.Empty;
    }


    private static Dictionary<string, object> JsonToDictionary(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return [];
        }

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
