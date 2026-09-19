namespace BotSharp.Abstraction.MLTasks;

/// <summary>
/// A full duplex voice model that owns the conversation itself: it listens and speaks at the
/// same time, arbitrates interruptions internally, and delegates reasoning and tool calls to a
/// separate backend model.
///
/// It drives the same hub as <see cref="IRealTimeCompletion"/> and is used wherever a realtime
/// completer is expected, so the interface adds nothing to that contract. It exists to keep the
/// two families apart in DI: a live provider is never returned by
/// <c>GetServices&lt;IRealTimeCompletion&gt;()</c>, so it is reachable only through the agent's
/// live config, never as a realtime fallback.
/// </summary>
public interface ILiveCompletion : IRealTimeCompletion
{
}
