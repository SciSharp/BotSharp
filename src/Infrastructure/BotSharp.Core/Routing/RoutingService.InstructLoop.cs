using BotSharp.Abstraction.Infrastructures.Enums;
using BotSharp.Abstraction.Planning;
using BotSharp.Abstraction.Routing.Enums;
using BotSharp.Abstraction.Routing.Reasoning;

namespace BotSharp.Core.Routing;

public partial class RoutingService
{
    public async Task<RoleDialogModel> InstructLoop(RoleDialogModel message, List<RoleDialogModel> dialogs)
    {
        RoleDialogModel response = default;

        var agentService = _services.GetRequiredService<IAgentService>();
        var convService = _services.GetRequiredService<IConversationService>();
        var storage = _services.GetRequiredService<IConversationStorage>();

        _router = await agentService.LoadAgent(message.CurrentAgentId);

        var states = _services.GetRequiredService<IConversationStateService>();
        var executor = _services.GetRequiredService<IExecutor>();

        var reasoner = GetReasoner(_router);

        _context.Push(_router.Id);

        // Handle multi-language for input
        var agentSettings = _services.GetRequiredService<AgentSettings>();
        if (agentSettings.EnableTranslator)
        {
            var translator = _services.GetRequiredService<ITranslationService>();

            var language = states.GetState(StateConst.LANGUAGE, LanguageType.ENGLISH);
            if (language != LanguageType.ENGLISH)
            {
                message.SecondaryContent = message.Content;
                message.Content = await translator.Translate(_router, message.MessageId, message.Content,
                    language: LanguageType.ENGLISH,
                    clone: false);
            }
        }

        dialogs.Add(message);
        storage.Append(convService.ConversationId, message);

        // Get first instruction
        _router.TemplateDict["conversation"] = await GetConversationContent(dialogs);
        var inst = await reasoner.GetNextInstruction(_router, message.MessageId, dialogs);

        int loopCount = 1;
        while (true)
        {
            await HookEmitter.Emit<IRoutingHook>(_services, async hook =>
                await hook.OnRoutingInstructionReceived(inst, message)
            );

            // Save states
            states.SaveStateByArgs(inst.Arguments);

#if DEBUG
            Console.WriteLine($"*** Next Instruction *** {inst}");
#else
            _logger.LogInformation($"*** Next Instruction *** {inst}");
#endif
            await reasoner.AgentExecuting(_router, inst, message, dialogs);

            // Handover to Task Agent
            if (inst.HandleDialogsByPlanner)
            {
                var dialogWithoutContext = reasoner.BeforeHandleContext(inst, message, dialogs);
                response = await executor.Execute(this, inst, message, dialogWithoutContext);
                reasoner.AfterHandleContext(dialogs, dialogWithoutContext);
            }
            else
            {
                response = await executor.Execute(this, inst, message, dialogs);
            }

            await reasoner.AgentExecuted(_router, inst, response, dialogs);

            if (loopCount >= reasoner.MaxLoopCount || _context.IsEmpty)
            {
                break;
            }

            // Get next instruction from Planner
            _router.TemplateDict["conversation"] = await GetConversationContent(dialogs);
            inst = await reasoner.GetNextInstruction(_router, message.MessageId, dialogs);
            loopCount++;
        }

        return response;
    }

    public IRoutingReasoner GetReasoner(Agent router)
    {
        var rule = router.RoutingRules.FirstOrDefault(x => x.Type == RuleType.Reasoner);

        if (rule == null)
        {
            _logger.LogError($"Can't find any reasoner");
            return _services.GetServices<IRoutingReasoner>().First(x => x.Name == "Naive Reasoner");
        }

        var reasoner = _services.GetServices<IRoutingReasoner>().FirstOrDefault(x => x.GetType().Name.EndsWith(rule.Field));

        if (reasoner == null)
        {
            _logger.LogError($"Can't find specific reasoner named {rule.Field}");
            // Default use NaiveReasoner
            return _services.GetServices<IRoutingReasoner>().First(x => x.Name == "Naive Reasoner");
        }

        return reasoner;
    }
}
