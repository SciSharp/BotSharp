namespace BotSharp.Plugin.ExcelHandler.Hooks;

public class ExcelHandlerHook : AgentHookBase, IAgentHook
{
    private const string HANDLER_EXCEL = "handle_excel_request";

    public override string SelfId => string.Empty;

    public ExcelHandlerHook(IServiceProvider services, AgentSettings settings) : base(services, settings)
    {
    }

    public override void OnAgentLoaded(Agent agent)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        var isConvMode = conv.IsConversationMode();
        var isEnabled = !agent.Utilities.IsNullOrEmpty() && agent.Utilities.Contains(UtilityName.ExcelHandler);

        if (isEnabled && isConvMode)
        {
            AddUtility(agent, HANDLER_EXCEL);
        }

        base.OnAgentLoaded(agent);
    }

    private void AddUtility(Agent agent, string functionName)
    {
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

    private (string, FunctionDef?) GetPromptAndFunction(string functionName)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var agent = db.GetAgent(BuiltInAgentId.UtilityAssistant);
        var prompt = agent?.Templates?.FirstOrDefault(x => x.Name.IsEqualTo($"{functionName}.fn"))?.Content ?? string.Empty;
        var loadAttachmentFn = agent?.Functions?.FirstOrDefault(x => x.Name.IsEqualTo(functionName));
        return (prompt, loadAttachmentFn);
    }
}

