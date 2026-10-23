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
}
