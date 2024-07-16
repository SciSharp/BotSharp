using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Agents.Settings;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Repositories;

namespace BotSharp.Plugin.FileHandler.Hooks;

public class FileHandlerHook : AgentHookBase, IAgentHook
{
    private const string READ_IMAGE_FN = "read_image";
    private const string READ_PDF_FN = "read_pdf";

    public override string SelfId => string.Empty;

    public FileHandlerHook(IServiceProvider services, AgentSettings settings) : base(services, settings)
    {
    }

    public override void OnAgentLoaded(Agent agent)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        var isConvMode = conv.IsConversationMode();

        if (isConvMode)
        {
            AddUtility(agent, UtilityName.ImageReader, READ_IMAGE_FN);
            AddUtility(agent, UtilityName.PdfReader, READ_PDF_FN);
        }

        base.OnAgentLoaded(agent);
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
}
