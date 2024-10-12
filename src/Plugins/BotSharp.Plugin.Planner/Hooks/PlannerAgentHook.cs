namespace BotSharp.Plugin.Planner.Hooks;

public class PlannerAgentHook : AgentHookBase
{
    public override string SelfId => BuiltInAgentId.Planner;

    public PlannerAgentHook(IServiceProvider services, AgentSettings settings)
        : base(services, settings)
    {
    }

    public override bool OnInstructionLoaded(string template, Dictionary<string, object> dict)
    {
        var knowledgeHooks = _services.GetServices<IKnowledgeHook>();

        // Get global knowledges
        var Knowledges = new List<string>();
        foreach (var hook in knowledgeHooks)
        {
            var k = hook.GetGlobalKnowledges(new RoleDialogModel(AgentRole.User, template)
            {
                CurrentAgentId = BuiltInAgentId.Planner
            }).Result;
            Knowledges.AddRange(k);
        }
        dict["global_knowledges"] = Knowledges;

        return true;
    }

    public override void OnAgentLoaded(Agent agent)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        var isConvMode = conv.IsConversationMode();
        var isEnabled = !agent.Utilities.IsNullOrEmpty() && agent.Utilities.Contains(UtilityName.TwoStagePlanner);

        if (isConvMode && isEnabled)
        {
            var (prompt, fn) = GetPromptAndFunction("plan_primary_stage");
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

            (prompt, fn) = GetPromptAndFunction("plan_secondary_stage");
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

            (prompt, fn) = GetPromptAndFunction("plan_summary");
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

    private (string, FunctionDef?) GetPromptAndFunction(string functionName)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var agent = db.GetAgent(BuiltInAgentId.UtilityAssistant);
        var prompt = agent?.Templates?.FirstOrDefault(x => x.Name.IsEqualTo($"{functionName}.fn"))?.Content ?? string.Empty;
        var loadAttachmentFn = agent?.Functions?.FirstOrDefault(x => x.Name.IsEqualTo(functionName));
        return (prompt, loadAttachmentFn);
    }
}
