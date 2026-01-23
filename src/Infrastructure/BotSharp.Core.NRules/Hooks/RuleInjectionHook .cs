using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Routing.Models;
using BotSharp.Abstraction.Rules;
using BotSharp.Core.NRules.Models;

namespace BotSharp.Core.NRules.Hooks;

public class RuleInjectionHook : ConversationHookBase
{

    private readonly IUniversalParsingEngine _upeService;
    private readonly IConversationService _covService; 
    private readonly List<string> _internalFunctions = new() { 
        "route_to_agent" ,
        "util-routing-fallback_to_router" ,
        "human_intervention_needed",
        "response_to_user"
    };

    public RuleInjectionHook(IUniversalParsingEngine upeEngine, IConversationService conversationService)
    {
        _upeService = upeEngine;
        _covService = conversationService;
    }


    // Scenario B: Handle function execution result (corresponds to FunctionExecutedFact)
    public override async Task OnFunctionExecuted(RoleDialogModel message, InvokeFunctionOptions? options = null)
    {  
        if (message.FunctionName != null)
        {
            if (_internalFunctions.Contains(message.FunctionName))
            {
                return;
            }
            else
            {
                var ruleContext = await _upeService.GetContextAsync(_covService.ConversationId);

                // message.Content contains the function execution result in JSON
                var funcFact = new FunctionExecutedFact
                {
                    FunctionName = message.FunctionName,
                    Output = message.Content,
                    IsSuccess = true
                };

                ruleContext.Insert(funcFact);
                ruleContext.Fire();
            }
        }
    }

    public override async Task OnMessageReceived(RoleDialogModel message)
    {
        // 1. Get RuleContext
        var context = await _upeService.GetContextAsync(_covService.ConversationId);

        // 2. Hydrate: Load historical state
        await context.HydrateFactsAsync();

        // 3. Inject current message as a fact
        var inputFact = new UserInputFact { Text = message.Content, Role = message.Role };
        context.Insert(inputFact);

        // 4. Trigger inference
        context.Fire();

        // 5. Persist: Handle inference results
        await context.PersistStateAsync(); 

        // 6. Check if interception is needed (e.g., rule triggered blocking logic)
        var blockAction = context.Session.Query<BlockAction>().FirstOrDefault();
        if (blockAction != null)
        {
            message.StopCompletion = blockAction.StopCompletion;
            message.Content = blockAction.Reason;
        }
    }
}