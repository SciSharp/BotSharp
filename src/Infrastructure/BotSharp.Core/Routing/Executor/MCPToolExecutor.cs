using BotSharp.Abstraction.Routing.Executor;
using BotSharp.Core.MCP.Managers;
using BotSharp.Core.MessageHub;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace BotSharp.Core.Routing.Executor;

public class McpToolExecutor : IFunctionExecutor
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
            Dictionary<string, object?> argDict = JsonToDictionary(message.FunctionArgs);

            var clientManager = _services.GetRequiredService<McpClientManager>();

            // The client is a session of its own, so this call owns it. Disposing closes the
            // session on the server; the connection underneath it stays in the factory's pool.
            await using var client = await clientManager.GetMcpClientAsync(_mcpServerId);

            if (client == null)
            {
                message.Content = $"MCP client for server {_mcpServerId} not found.";
                return false;
            }

            // Call the tool through mcpdotnet, relaying whatever progress it reports along the
            // way — see ToolProgressIndicator for why the reporter has to be supplied here.
            var result = await client.CallToolAsync(
                _functionName,
                !argDict.IsNullOrEmpty() ? argDict : [],
                progress: ToolProgressIndicator.For(_services, message));

            // Extract the text content from the result
            var json = string.Join("\n", result.Content.Where(c => c is TextContentBlock).Select(c => ((TextContentBlock)c).Text));

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

    public Task<string> GetIndicatorAsync(RoleDialogModel message)
    {
        return Task.FromResult(message.Indication ?? string.Empty);
    }

    /// <summary>
    /// Turns a tool's <c>notifications/progress</c> into indications, so a long call can say what
    /// it is doing while it does it.
    /// <para>
    /// WHY THIS EXISTS. RoutingService pushes one indication when a function starts, and for a
    /// tool that returns in a second that is the whole story. An MCP tool driving a browser takes
    /// minutes, and that single line — "Working out the steps" — was all the chat had to show for
    /// the entire run: no way to tell a task making progress from one that had wedged.
    /// </para>
    /// <para>
    /// Supplying the reporter is also what MAKES a server report. The SDK only attaches a
    /// <c>progressToken</c> to the request when this is non-null, and a server with no token to
    /// answer has nowhere to send notifications — computer-autoplay's <c>await_web_task</c>
    /// returns early from its own reporter for exactly that reason. So the argument is not an
    /// optimisation of an existing stream; it is what opens it.
    /// </para>
    /// <para>
    /// Nothing is required of a server that does not report progress: no notifications arrive,
    /// <see cref="Report"/> is never called, and the call behaves as it did before.
    /// </para>
    /// </summary>
    private sealed class ToolProgressIndicator : IProgress<ProgressNotificationValue>
    {
        private readonly MessageHub<HubObserveData<RoleDialogModel>> _hub;
        private readonly string _conversationId;
        private readonly RoleDialogModel _message;

        private ToolProgressIndicator(
            MessageHub<HubObserveData<RoleDialogModel>> hub,
            string conversationId,
            RoleDialogModel message)
        {
            _hub = hub;
            _conversationId = conversationId;
            _message = message;
        }

        /// <summary>
        /// A reporter for this call, or null when there is no conversation to report into — a
        /// tool invoked outside one, from a task or a test. Null is the right answer there rather
        /// than a reporter that drops everything: it also tells the server not to bother sending.
        /// <para>
        /// The conversation id is read HERE, on the thread that starts the call, and captured.
        /// <see cref="Report"/> runs on whichever thread the MCP transport is reading on, and
        /// resolving a scoped service from there to ask again would be a race for a value that
        /// cannot change during the call.
        /// </para>
        /// </summary>
        public static ToolProgressIndicator? For(IServiceProvider services, RoleDialogModel message)
        {
            var conversationId = services.GetRequiredService<IConversationService>().ConversationId;
            if (string.IsNullOrWhiteSpace(conversationId))
            {
                return null;
            }

            var hub = services.GetRequiredService<MessageHub<HubObserveData<RoleDialogModel>>>();
            return new ToolProgressIndicator(hub, conversationId, message);
        }

        /// <summary>
        /// Pushes one indication per reported step.
        /// <para>
        /// Deliberately synchronous, which is why this is a hand-written IProgress rather than a
        /// <see cref="Progress{T}"/>: Progress&lt;T&gt; queues every report to the thread pool
        /// independently, so two steps reported together can arrive out of order — and the one
        /// that arrives last is the one left on screen. Pushing inline keeps the order the server
        /// sent them in.
        /// </para>
        /// </summary>
        public void Report(ProgressNotificationValue value)
        {
            // A bare count with no message is a progress BAR's input, not a sentence, and the
            // chat has nowhere to put it. Servers that only send numbers are simply not relayed.
            if (string.IsNullOrWhiteSpace(value.Message))
            {
                return;
            }

            // Cloned: this is pushed to observers that read it, and the function's own message is
            // still being used by the call in flight. Its indication is not ours to overwrite.
            var indication = RoleDialogModel.From(_message);
            indication.Indication = value.Message;

            _hub.Push(new()
            {
                EventName = ChatEvent.OnIndicationReceived,
                Data = indication,
                RefId = _conversationId
            });
        }
    }


    private static Dictionary<string, object?> JsonToDictionary(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return [];
        }

        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;
        return JsonElementToDictionary(root);
    }

    private static Dictionary<string, object?> JsonElementToDictionary(JsonElement element)
    {
        Dictionary<string, object?> dictionary = [];

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
