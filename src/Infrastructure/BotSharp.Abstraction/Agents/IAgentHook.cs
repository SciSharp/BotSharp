using BotSharp.Abstraction.Functions.Models;

namespace BotSharp.Abstraction.Agents;

public interface IAgentHook
{
    Agent Agent { get; }
    void SetAget(Agent agent);

    /// <summary>
    /// Triggered before loading, you can change the returned id to switch agent.
    /// </summary>
    /// <param name="id">Agent Id</param>
    /// <returns></returns>
    bool OnAgentLoading(ref string id);


    bool OnInstructionLoaded(string template, Dictionary<string, object> dict);

    bool OnFunctionsLoaded(ref List<FunctionDef> functions);

    bool OnSamplesLoaded(ref string samples);

    /// <summary>
    /// Triggered when agent is loaded completely.
    /// </summary>
    /// <param name="agent"></param>
    /// <returns></returns>
    void OnAgentLoaded(Agent agent);
}
