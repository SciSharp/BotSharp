namespace BotSharp.Abstraction.Hooks;

public interface IHookBase
{
    /// <summary>
    /// Agent Id
    /// </summary>
    string SelfId => string.Empty;
    bool IsMatch(string agentId) => string.IsNullOrEmpty(SelfId) || SelfId == agentId;
}
