using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Functions;
using System.Text.Json.Serialization;

namespace BotSharp.Plugin.Membase.Functions;

public class UtilAdvanceWorkflowCursorFn : IFunctionCallback
{
    private readonly IServiceProvider _services;
    private readonly ILogger<UtilAdvanceWorkflowCursorFn> _logger;
    private readonly IConversationStateService _states;

    public UtilAdvanceWorkflowCursorFn(
        IServiceProvider services,
        ILogger<UtilAdvanceWorkflowCursorFn> logger,
        IConversationStateService states)
    {
        _services = services;
        _logger = logger;
        _states = states;
    }

    public string Name => "util-workflow-advance_workflow_cursor";

    public string Indication => "Advancing workflow step";

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<WorkflowCursorArgs>(message.FunctionArgs ?? "{}");
        var nextNodeId = args?.NextNodeId;
        message.Content = $"The next node id is '{nextNodeId}'";
        return true;
    }
}

public class WorkflowCursorArgs
{
    [JsonPropertyName("next_node_id")]
    public string NextNodeId { get; set; }
}