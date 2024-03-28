using BotSharp.Abstraction.Functions.Models;

namespace BotSharp.Abstraction.Conversations;

public interface IConversationHook
{
    int Priority { get; }
    Agent Agent { get; }
    List<RoleDialogModel> Dialogs { get; }
    IConversationHook SetAgent(Agent agent);

    Conversation Conversation { get; }
    IConversationHook SetConversation(Conversation conversation);

    /// <summary>
    /// Triggered when user connects with agent first time.
    /// This hook is the good timing to show welcome infomation.
    /// </summary>
    /// <param name="conversation"></param>
    /// <returns></returns>
    Task OnUserAgentConnectedInitially(Conversation conversation);

    /// <summary>
    /// Triggered once for every new conversation.
    /// </summary>
    /// <param name="conversation"></param>
    /// <returns></returns>
    Task OnConversationInitialized(Conversation conversation);

    /// <summary>
    /// Triggered when dialog history is loaded.
    /// </summary>
    /// <param name="dialogs"></param>
    /// <returns></returns>
    Task OnDialogsLoaded(List<RoleDialogModel> dialogs);

    /// <summary>
    /// Triggered when every dialog record is loaded
    /// It can be used to populate extra data point before presenting to user.
    /// </summary>
    /// <param name="dialog"></param>
    /// <returns></returns>
    Task OnDialogRecordLoaded(RoleDialogModel dialog);

    Task OnStateLoaded(ConversationState state);
    Task OnStateChanged(StateChangeModel stateChange);

    Task OnMessageReceived(RoleDialogModel message);
    Task OnPostbackMessageReceived(RoleDialogModel message, PostbackMessageModel replyMsg);

    /// <summary>
    /// Triggered before LLM calls function.
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    Task OnFunctionExecuting(RoleDialogModel message);

    /// <summary>
    /// Triggered when the function calling completed.
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    Task OnFunctionExecuted(RoleDialogModel message);

    Task OnResponseGenerated(RoleDialogModel message);

    /// <summary>
    /// LLM detected the current task is completed.
    /// It's useful for the situation of multiple tasks in the same conversation.
    /// </summary>
    /// <param name="conversation"></param>
    /// <returns></returns>
    Task OnTaskCompleted(RoleDialogModel message);

    /// <summary>
    /// LLM detected the whole conversation is going to be end.
    /// </summary>
    /// <param name="conversation"></param>
    /// <returns></returns>
    Task OnConversationEnding(RoleDialogModel message);

    /// <summary>
    /// LLM can't handle user's request or user requests human being to involve.
    /// </summary>
    /// <param name="conversation"></param>
    /// <returns></returns>
    Task OnHumanInterventionNeeded(RoleDialogModel message);

    /// <summary>
    /// Delete message in a conversation
    /// </summary>
    /// <param name="conversationId"></param>
    /// <param name="messageId"></param>
    /// <returns></returns>
    Task OnMessageDeleted(string conversationId, string messageId);

    /// <summary>
    /// Brakpoint updated
    /// </summary>
    /// <param name="conversationId"></param>
    /// <returns></returns>
    Task OnBreakpointUpdated(string conversationId, bool resetStates);
}
