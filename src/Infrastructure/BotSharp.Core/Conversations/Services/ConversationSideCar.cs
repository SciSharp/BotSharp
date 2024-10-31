using BotSharp.Abstraction.Conversations.Enums;
using BotSharp.Abstraction.Models;

namespace BotSharp.Core.Conversations.Services;

public class ConversationSideCar : IConversationSideCar
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ConversationSideCar> _logger;

    private Stack<ConversationContext> contextStack = new();

    private bool enabled = false;

    public ConversationSideCar(
        IServiceProvider services,
        ILogger<ConversationSideCar> logger)
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
        if (enabled)
        {
            var top = contextStack.Peek();
            top.Dialogs.AddRange(messages);
        }
        else
        {
            var db = _services.GetRequiredService<IBotSharpRepository>();
            db.AppendConversationDialogs(conversationId, messages);
        }
    }

    public List<DialogElement> GetConversationDialogs(string conversationId)
    {
        if (enabled)
        {
            return contextStack.Peek().Dialogs;
        }
        else
        {
            var db = _services.GetRequiredService<IBotSharpRepository>();
            return db.GetConversationDialogs(conversationId);
        }
    }

    public void UpdateConversationBreakpoint(string conversationId, ConversationBreakpoint breakpoint)
    {
        if (enabled)
        {
            var top = contextStack.Peek().Breakpoints;
            top.Add(breakpoint);
        }
        else
        {
            var db = _services.GetRequiredService<IBotSharpRepository>();
            db.UpdateConversationBreakpoint(conversationId, breakpoint);
        }
    }

    public ConversationBreakpoint? GetConversationBreakpoint(string conversationId)
    {
        if (enabled)
        {
            var top = contextStack.Peek().Breakpoints;
            return top.LastOrDefault();
        }
        else
        {
            var db = _services.GetRequiredService<IBotSharpRepository>();
            return db.GetConversationBreakpoint(conversationId);
        }
    }

    public async Task<RoleDialogModel> Execute(string agentId, string text,
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
        enabled = false;
    }
}