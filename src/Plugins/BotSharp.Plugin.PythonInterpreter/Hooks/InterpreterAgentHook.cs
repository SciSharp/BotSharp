namespace BotSharp.Plugin.PythonInterpreter.Hooks;

public class InterpreterAgentHook : AgentHookBase
{
    private static string FUNCTION_NAME = "python_interpreter";

    public override string SelfId => string.Empty;

    public InterpreterAgentHook(IServiceProvider services, AgentSettings settings)
        : base(services, settings)
    {
    }
    public override void OnAgentLoaded(Agent agent)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        var isConvMode = conv.IsConversationMode();
        var isEnabled = !agent.Utilities.IsNullOrEmpty() && agent.Utilities.Contains(UtilityName.PythonInterpreter);

        if (isConvMode && isEnabled)
        {
            var (prompt, fn) = GetPromptAndFunction();
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

        base.OnAgentLoaded(agent);
    }

    private (string, FunctionDef?) GetPromptAndFunction()
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var agent = db.GetAgent(BuiltInAgentId.UtilityAssistant);
        var prompt = agent?.Templates?.FirstOrDefault(x => x.Name.IsEqualTo($"{FUNCTION_NAME}.fn"))?.Content ?? string.Empty;
        var loadAttachmentFn = agent?.Functions?.FirstOrDefault(x => x.Name.IsEqualTo(FUNCTION_NAME));
        return (prompt, loadAttachmentFn);
    }
}
