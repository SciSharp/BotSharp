using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Planning;
using BotSharp.Abstraction.Routing;
using BotSharp.Core.Infrastructures;

namespace BotSharp.Plugin.SqlDriver.Hooks;

public class SqlDriverPlanningHook : IPlanningHook
{
    private readonly IServiceProvider _services;

    public SqlDriverPlanningHook(IServiceProvider services)
    {
        _services = services;
    }

    public async Task OnPlanningCompleted(string planner, RoleDialogModel msg)
    {
        var settings = _services.GetRequiredService<SqlDriverSetting>();
        if (!settings.ExecuteSqlSelectAutonomous)
        {
            return;
        }

        var conv = _services.GetRequiredService<IConversationService>();
        var wholeDialogs = conv.GetDialogHistory();
        wholeDialogs.Add(RoleDialogModel.From(msg));
        wholeDialogs.Add(RoleDialogModel.From(msg, AgentRole.User, $"call execute_sql to run query, set formatting_result as {settings.FormattingResult}"));

        var agent = await _services.GetRequiredService<IAgentService>().LoadAgent(BuiltInAgentId.SqlDriver);
        var completion = CompletionProvider.GetChatCompletion(_services,
            provider: agent.LlmConfig.Provider,
            model: agent.LlmConfig.Model);

        var response = await completion.GetChatCompletions(agent, wholeDialogs);

        // Invoke "execute_sql"
        var routing = _services.GetRequiredService<IRoutingService>();
        await routing.InvokeFunction(response.FunctionName, response);
        msg.CurrentAgentId = agent.Id;
        msg.FunctionName = response.FunctionName;
        msg.FunctionArgs = response.FunctionArgs;
        msg.Content = response.Content;
        msg.StopCompletion = response.StopCompletion;

        /*var routing = _services.GetRequiredService<IRoutingService>();
        await routing.InvokeAgent(BuiltInAgentId.SqlDriver, wholeDialogs);*/
    }
}
