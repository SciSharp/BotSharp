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

    public async Task<string> GetSummaryAdditionalRequirements(string planner)
    {
        var settings = _services.GetRequiredService<SqlDriverSetting>();
        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.GetAgent(BuiltInAgentId.Planner);
        return agent.Templates.FirstOrDefault(x => x.Name == $"database.summarize.{settings.DatabaseType.ToLower()}")?.Content ?? string.Empty;
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
        wholeDialogs.Add(RoleDialogModel.From(msg, AgentRole.User, "use execute_sql to run query"));

        var agent = await _services.GetRequiredService<IAgentService>().LoadAgent("beda4c12-e1ec-4b4b-b328-3df4a6687c4f");

        var completion = CompletionProvider.GetChatCompletion(_services,
            provider: agent.LlmConfig.Provider,
            model: agent.LlmConfig.Model);

        var response = await completion.GetChatCompletions(agent, wholeDialogs);
        var routing = _services.GetRequiredService<IRoutingService>();
        await routing.InvokeFunction(response.FunctionName, response);
        msg.CurrentAgentId = agent.Id;
        msg.FunctionName = response.FunctionName;
        msg.FunctionArgs = response.FunctionArgs;
        msg.Content = response.Content;
    }
}
