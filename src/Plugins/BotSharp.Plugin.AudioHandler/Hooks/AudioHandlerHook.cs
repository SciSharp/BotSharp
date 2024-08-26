using BotSharp.Abstraction.Agents.Settings;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Repositories;

namespace BotSharp.Plugin.AudioHandler.Hooks;

public class AudioHandlerHook : AgentHookBase, IAgentHook
{
    private const string HANDLER_AUDIO = "handle_audio_request";

    public override string SelfId => string.Empty;

    public AudioHandlerHook(IServiceProvider services, AgentSettings settings) : base(services, settings)
    {
    }

    public override void OnAgentLoaded(Agent agent)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        var isConvMode = conv.IsConversationMode();
        var isEnabled = !agent.Utilities.IsNullOrEmpty() && agent.Utilities.Contains(UtilityName.AudioHandler);

        if (isEnabled && isConvMode)
        {
            AddUtility(agent, UtilityName.AudioHandler, HANDLER_AUDIO);
        }

        base.OnAgentLoaded(agent);
    }

    private void AddUtility(Agent agent, string utility, string functionName)
    {
        if (!IsEnableUtility(agent, utility)) return;

        var (prompt, fn) = GetPromptAndFunction(functionName);
        if (fn != null)
        {
            if (!string.IsNullOrWhiteSpace(prompt))
            {
                agent.Instruction += $"\r\n\r\n{prompt}\r\n\r\n";
            }

            if (agent.Functions == null)
            {
                agent.Functions = new List<FunctionDef> { fn };
            }
            else
            {
                agent.Functions.Add(fn);
            }
        }
    }

    private bool IsEnableUtility(Agent agent, string utility)
    {
        return !agent.Utilities.IsNullOrEmpty() && agent.Utilities.Contains(utility);
    }

    private (string, FunctionDef?) GetPromptAndFunction(string functionName)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var agent = db.GetAgent(BuiltInAgentId.UtilityAssistant);
        var prompt = agent?.Templates?.FirstOrDefault(x => x.Name.IsEqualTo($"{functionName}.fn"))?.Content ?? string.Empty;
        var loadAttachmentFn = agent?.Functions?.FirstOrDefault(x => x.Name.IsEqualTo(functionName));
        return (prompt, loadAttachmentFn);
    }
}
