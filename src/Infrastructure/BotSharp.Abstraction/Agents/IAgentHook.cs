using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Hooks;

namespace BotSharp.Abstraction.Agents;

public interface IAgentHook : IHookBase
{
    Agent Agent { get; }
    void SetAgent(Agent agent);

    /// <summary>
    /// Triggered when agent is loading.
    /// Return different agent id for redirection purpose.
    /// </summary>
    /// <param name="id">Agent Id</param>
    /// <returns>New agent id if redirection is needed, null otherwise</returns>
    Task<string?> OnAgentLoading(string id);

    Task<bool> OnInstructionLoaded(string template, IDictionary<string, object> dict);

    Task<bool> OnFunctionsLoaded(List<FunctionDef> functions);

    Task<bool> OnSamplesLoaded(List<string> samples);

    Task OnAgentUtilityLoaded(Agent agent);

    Task OnAgentMcpToolLoaded(Agent agent);

    /// <summary>
    /// Triggered when agent is loaded completely.
    /// </summary>
    /// <param name="agent"></param>
    /// <returns></returns>
    Task OnAgentLoaded(Agent agent);
}
