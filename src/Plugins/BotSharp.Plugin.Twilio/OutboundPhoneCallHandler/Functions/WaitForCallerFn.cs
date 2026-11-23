using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Enums;
using BotSharp.Plugin.Twilio.Models;
using Task = System.Threading.Tasks.Task;

namespace BotSharp.Plugin.Twilio.OutboundPhoneCallHandler.Functions;

/// <summary>
/// The other party on the phone says they need a moment (logging in, reviewing, looking something up): the agent says one
/// short line and then waits quietly, instead of reading its request out again and asking "I didn't get anything" every
/// few seconds.
/// </summary>
/// <remarks>
/// Turns on the relay's quiet wait (<see cref="ConversationRelayStates.QuietWait"/>): the session stops its idle prompts
/// and checks in softly each minute instead. The wait lasts until the other party speaks again, which the conversation
/// processor handles at the start of that turn (<see cref="EndWait"/>).
/// </remarks>
public class WaitForCallerFn : IFunctionCallback
{
    // Twilio:PromptTexts entry for what is said, like the session's own prompts
    private const string PromptName = "wait-for-caller";
    private const string DefaultText = "Sure, take your time.";

    private readonly IConversationStateService _states;
    private readonly TwilioSetting _setting;

    public string Name => "util-twilio-wait_for_caller";
    public string Indication => "waiting for the caller";

    public WaitForCallerFn(IConversationStateService states, TwilioSetting setting)
    {
        _states = states;
        _setting = setting;
    }

    public Task<bool> Execute(RoleDialogModel message)
    {
        _states.SetState(ConversationRelayStates.QuietWait, "true", source: StateSource.Application);
        _states.SetState(ConversationRelayStates.WaitForCallerRequested, "true", source: StateSource.Application);

        // Said as it is, so the model has nothing to add to it
        message.Content = _setting.PromptTexts.TryGetValue(PromptName, out var text) && !string.IsNullOrWhiteSpace(text) ? text : DefaultText;
        message.StopCompletion = true;
        return Task.FromResult(true);
    }

    /// <summary>
    /// Ends the wait this function started: called as a turn starts, which is the other party speaking again. A quiet wait
    /// a flow set itself (the caller filling in a form) is left alone.
    /// </summary>
    public static void EndWait(IConversationStateService states)
    {
        if (!states.ContainsState(ConversationRelayStates.WaitForCallerRequested))
        {
            return;
        }

        states.RemoveState(ConversationRelayStates.WaitForCallerRequested);
        states.RemoveState(ConversationRelayStates.QuietWait);
    }
}
