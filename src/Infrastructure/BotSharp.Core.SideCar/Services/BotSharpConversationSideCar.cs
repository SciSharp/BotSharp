using BotSharp.Core.Infrastructures;

namespace BotSharp.Core.SideCar.Services;

public class BotSharpConversationSideCar : IConversationSideCar
{
    private readonly IServiceProvider _services;
    private readonly ILogger<BotSharpConversationSideCar> _logger;

    private Stack<ConversationContext> contextStack = new();

    private bool enabled = false;

    public string Provider => "botsharp";

    public BotSharpConversationSideCar(
        IServiceProvider services,
        ILogger<BotSharpConversationSideCar> logger)
    {
        _services = services;
        _logger = logger;
    }

    public bool IsEnabled()
    {
        return enabled;
    }

    public void AppendConversationDialogs(string conversationId, List<DialogElement> messages)
    {
        if (contextStack.IsNullOrEmpty()) return;

        var top = contextStack.Peek();
        top.Dialogs.AddRange(messages);
    }

    public List<DialogElement> GetConversationDialogs(string conversationId)
    {
        if (contextStack.IsNullOrEmpty())
        {
            return new List<DialogElement>();
        }

        return contextStack.Peek().Dialogs;
    }

    public void UpdateConversationBreakpoint(string conversationId, ConversationBreakpoint breakpoint)
    {
        if (contextStack.IsNullOrEmpty()) return;

        var top = contextStack.Peek().Breakpoints;
        top.Add(breakpoint);
    }

    public ConversationBreakpoint? GetConversationBreakpoint(string conversationId)
    {
        if (contextStack.IsNullOrEmpty())
        {
            return null;
        }

        var top = contextStack.Peek().Breakpoints;
        return top.LastOrDefault();
    }

    public async Task<RoleDialogModel> SendMessage(string agentId, string text,
        PostbackMessageModel? postback = null, List<MessageState>? states = null)
    {
        BeforeExecute();
        var response = await InnerExecute(agentId, text, postback, states);
        AfterExecute();
        return response;
    }

    private async Task<RoleDialogModel> InnerExecute(string agentId, string text,
        PostbackMessageModel? postback = null, List<MessageState>? states = null)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        var routing = _services.GetRequiredService<IRoutingService>();
        var state = _services.GetRequiredService<IConversationStateService>();

        var inputMsg = new RoleDialogModel(AgentRole.User, text);
        routing.Context.SetMessageId(conv.ConversationId, inputMsg.MessageId);
        states?.ForEach(x => state.SetState(x.Key, x.Value, activeRounds: x.ActiveRounds, source: StateSource.External));

        var response = new RoleDialogModel(AgentRole.Assistant, string.Empty);
        await conv.SendMessage(agentId, inputMsg,
            replyMessage: postback,
            async msg =>
            {
                response.Content = !string.IsNullOrEmpty(msg.SecondaryContent) ? msg.SecondaryContent : msg.Content;
                response.FunctionName = msg.FunctionName;
                response.RichContent = msg.SecondaryRichContent ?? msg.RichContent;
                response.Instruction = msg.Instruction;
                response.Data = msg.Data;
            });

        return response;
    }

    private void BeforeExecute()
    {
        enabled = true;
        var state = _services.GetRequiredService<IConversationStateService>();
        var routing = _services.GetRequiredService<IRoutingService>();

        var node = new ConversationContext
        {
            State = state.GetCurrentState(),
            Dialogs = new(),
            Breakpoints = new(),
            RecursiveCounter = routing.Context.GetRecursiveCounter(),
            RoutingStack = routing.Context.GetAgentStack()
        };
        contextStack.Push(node);

        // Reset
        state.ResetCurrentState();
        routing.Context.ResetRecursiveCounter();
        routing.Context.ResetAgentStack();
        Utilities.ClearCache();
    }

    private void AfterExecute()
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        var routing = _services.GetRequiredService<IRoutingService>();

        var node = contextStack.Pop();

        // Recover
        state.SetCurrentState(node.State);
        routing.Context.SetRecursiveCounter(node.RecursiveCounter);
        routing.Context.SetAgentStack(node.RoutingStack);
        Utilities.ClearCache();
        enabled = false;
    }
}