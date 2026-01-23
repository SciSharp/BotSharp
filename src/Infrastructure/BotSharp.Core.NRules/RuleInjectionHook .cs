using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Routing.Models;
using NRules;

namespace BotSharp.Core.NRules;

public class RuleInjectionHook : ConversationHookBase
{

    private readonly INRulesEngineService _rulesService;
    private readonly IConversationService _covService; 

    public RuleInjectionHook(INRulesEngineService nRulesEngine, IConversationService conversationService)
    {
        _rulesService = nRulesEngine;
        _covService = conversationService;
    }


    // 场景 B：处理函数执行结果 (对应 FunctionExecutedFact)
    public override async Task OnFunctionExecuted(RoleDialogModel message, InvokeFunctionOptions? options = null)
    {
        var ruleContext = await _rulesService.GetContextAsync(_covService.ConversationId);

        if (message.FunctionName != null && message.FunctionName.StartsWith("internal."))
        {

            // 假设 message.Content 包含了函数的执行结果 JSON
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

    public override async Task OnMessageReceived(RoleDialogModel message)
    {
        // 1. 获取 RuleContext
        var context = await _rulesService.GetContextAsync(_covService.ConversationId);

        // 2. 水合：加载历史状态
        await context.HydrateFactsAsync();

        // 3. 注入当前消息作为事实
        var inputFact = new UserInputFact { Text = message.Content, Role = message.Role };
        context.Insert(inputFact);

        // 4. 触发推理
        context.Fire();

        // 5. 持久化：处理推理结果
        await context.PersistStateAsync(); 

        // 6. 检查是否需要拦截 (例如规则触发了阻断逻辑)
        var blockAction = context.Session.Query<BlockAction>().FirstOrDefault();
        if (blockAction != null)
        {
            message.StopCompletion = blockAction.StopCompletion;
            message.Content = blockAction.Reason;
        }
    }
}