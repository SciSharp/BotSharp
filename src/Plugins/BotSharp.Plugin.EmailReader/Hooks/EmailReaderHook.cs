using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Agents.Settings;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Repositories;
using BotSharp.Plugin.EmailReader.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Plugin.EmailReader.Hooks;

public class EmailReaderHook : AgentHookBase
{
    private static string FUNCTION_NAME = "handle_email_reader";

    public override string SelfId => string.Empty;

    public EmailReaderHook(IServiceProvider services, AgentSettings settings)
        : base(services, settings)
    {
    }
    public override void OnAgentLoaded(Agent agent)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        var isConvMode = conv.IsConversationMode();
        var isEnabled = !agent.Utilities.IsNullOrEmpty() && agent.Utilities.Contains(UtilityName.EmailReader);

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
