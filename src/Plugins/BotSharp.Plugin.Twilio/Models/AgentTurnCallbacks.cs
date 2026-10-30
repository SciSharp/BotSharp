using Task = System.Threading.Tasks.Task;

namespace BotSharp.Plugin.Twilio.Models;

/// <summary>
/// What a phone flow wants to hear about while the agent works on one caller message.
/// </summary>
public class AgentTurnCallbacks
{
    /// <summary>
    /// A tool-call indication, e.g. "#hold-on-1" or a sentence to say while the tool runs
    /// </summary>
    public Func<string, Task>? OnIndication { get; init; }

    /// <summary>
    /// When set, the turn streams: called for each piece of reply text as the model writes it
    /// </summary>
    public Func<string, Task>? OnStreamText { get; init; }

    /// <summary>
    /// Called when the model starts writing a new response, e.g. after a tool call
    /// </summary>
    public Func<Task>? OnStreamStarted { get; init; }

    /// <summary>
    /// Called when a response the model wrote ends in a tool call: what it said before the call is complete,
    /// and the tool may take a while before anything else is said
    /// </summary>
    public Func<Task>? OnStreamEndedWithToolCall { get; init; }

    /// <summary>
    /// The last say over what the conversation history keeps of the reply: called with the reply the agent wrote,
    /// returns what is stored in its place. Lets a flow record a reply the caller cut off as the part they heard.
    /// </summary>
    public Func<string, string>? RewriteReply { get; init; }

    /// <summary>
    /// Set by the turn once it can read the conversation's states: whether what the agent says now must play to the end
    /// (<see cref="ConversationRelayStates.KeepUninterrupted"/>).
    /// </summary>
    /// <remarks>
    /// Read before each piece of the reply goes out, so a flow can turn it on part way through a turn, e.g. once a
    /// confirmation number is coming up. Only valid while the turn runs.
    /// </remarks>
    public Func<bool>? KeepUninterrupted { get; set; }

    /// <summary>
    /// Called on the turn once it has read the conversation's states (<see cref="KeepUninterrupted"/> is set), before the
    /// agent hears the caller's words
    /// </summary>
    public Func<Task>? OnTurnStarted { get; init; }

    private readonly object _cancelSync = new();
    private Action? _cancel;
    private bool _cancelRequested;

    /// <summary>
    /// The flow no longer wants this turn's reply: the model stops writing it, and any response the turn would
    /// still ask the model for comes back empty. What already ran (hooks, a tool) stays done.
    /// </summary>
    /// <remarks>
    /// May be called before the turn can be cancelled; it is then cancelled as soon as it can be.
    /// </remarks>
    public void Cancel()
    {
        Action? cancel;
        lock (_cancelSync)
        {
            if (_cancelRequested)
            {
                return;
            }
            _cancelRequested = true;
            cancel = _cancel;
        }
        cancel?.Invoke();
    }

    /// <summary>
    /// Set by the turn once it can be cancelled; runs at once when <see cref="Cancel"/> came first
    /// </summary>
    public void OnCancel(Action cancel)
    {
        bool requested;
        lock (_cancelSync)
        {
            _cancel = cancel;
            requested = _cancelRequested;
        }
        if (requested)
        {
            cancel();
        }
    }
}
