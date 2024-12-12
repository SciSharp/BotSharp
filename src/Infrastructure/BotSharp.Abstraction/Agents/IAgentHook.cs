using BotSharp.Abstraction.Functions.Models;

namespace BotSharp.Abstraction.Agents;

public interface IAgentHook
{
    /// <summary>
    /// Agent Id
    /// </summary>
    string SelfId { get; }
    Agent Agent { get; }
    void SetAget(Agent agent);

    /// <summary>
    /// Triggered when agent is loading.
    /// Return different agent for redirection purpose.
    /// </summary>
    /// <param name="id">Agent Id</param>
    /// <returns></returns>
    bool OnAgentLoading(ref string id);

    bool OnInstructionLoaded(string template, Dictionary<string, object> dict);

    bool OnFunctionsLoaded(List<FunctionDef> functions);

    bool OnSamplesLoaded(List<string> samples);

    void OnAgentUtilityLoaded(Agent agent);

    /// <summary>
    /// Triggered when agent is loaded completely.
    /// </summary>
    /// <param name="agent"></param>
    /// <returns></returns>
    void OnAgentLoaded(Agent agent);

    void OnAgentLoadFilter(Agent agent);
}
