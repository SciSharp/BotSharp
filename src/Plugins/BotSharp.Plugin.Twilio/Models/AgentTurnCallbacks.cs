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
}
