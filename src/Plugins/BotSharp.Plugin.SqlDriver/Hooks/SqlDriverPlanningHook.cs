using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Messaging.Enums;
using BotSharp.Abstraction.Messaging.Models.RichContent.Template;
using BotSharp.Abstraction.Messaging.Models.RichContent;
using BotSharp.Abstraction.Messaging;
using BotSharp.Abstraction.Planning;
using BotSharp.Abstraction.Routing;
using BotSharp.Core.Infrastructures;
using System.Text.RegularExpressions;
using BotSharp.Plugin.SqlDriver.Interfaces;

namespace BotSharp.Plugin.SqlDriver.Hooks;

public class SqlDriverPlanningHook : IPlanningHook
{
    private readonly IServiceProvider _services;

    public SqlDriverPlanningHook(IServiceProvider services)
    {
        _services = services;
    }

    public async Task OnSourceCodeGenerated(string planner, RoleDialogModel msg, string language)
    {
        // envoke validate
        if (language != "sql")
        {
            return;
        }

        var routing = _services.GetRequiredService<IRoutingService>();
        await routing.InvokeFunction("validate_sql", msg);

        await HookEmitter.Emit<ISqlDriverHook>(_services, async (hook) =>
        {
            await hook.SqlGenerated(msg);
        });

        var settings = _services.GetRequiredService<SqlDriverSetting>();
        if (!settings.ExecuteSqlSelectAutonomous)
        {
            var conversationStateService = _services.GetRequiredService<IConversationStateService>();
            var conversationId = conversationStateService.GetConversationId();
            msg.PostbackFunctionName = "execute_sql";
            msg.RichContent = BuildRunQueryButton(conversationId, msg.Content);
            msg.StopCompletion = true;
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
        await routing.InvokeFunction(response.FunctionName, response);

        msg.CurrentAgentId = agent.Id;
        msg.FunctionName = response.FunctionName;
        msg.FunctionArgs = response.FunctionArgs;
        msg.Content = response.Content;
        msg.StopCompletion = response.StopCompletion;
    }

    public async Task OnPlanningCompleted(string planner, RoleDialogModel msg)
    {

    }

    public async Task<string> GetSummaryAdditionalRequirements(string planner, RoleDialogModel message)
    {
        var settings = _services.GetRequiredService<SqlDriverSetting>();
        var sqlHooks = _services.GetServices<ISqlDriverHook>();
        var agentService = _services.GetRequiredService<IAgentService>();

        var dbType = !sqlHooks.IsNullOrEmpty() ? sqlHooks.First().GetDatabaseType(message) : settings.DatabaseType;
        var agent = await agentService.LoadAgent(BuiltInAgentId.SqlDriver);

        return agent.Templates.FirstOrDefault(x => x.Name == $"database.summarize.{dbType}")?.Content ?? string.Empty;
    }

    private RichContent<IRichMessage> BuildRunQueryButton(string conversationId, string text)
    {
        string pattern = @"```sql\s*([\s\S]*?)\s*```";
        var sql = Regex.Match(text, pattern).Groups[1].Value;
        var state = _services.GetRequiredService<IConversationStateService>();
        var tmpTable = state.GetState("tmp_table");

        var elements = new List<ElementButton>() { };
        elements.Add(new ElementButton
        {
            Type = "text",
            Title = "Execute SQL Statement",
            Payload = sql,
            IsPrimary = true
        });

        if (tmpTable != string.Empty)
        {
            var deleteSql = $"DROP TABLE IF EXISTS {tmpTable};";
            elements.Add(new ElementButton
            {
                Type = "text",
                Title = "Delete Temp Table",
                Payload = deleteSql
            });
        }

        return new RichContent<IRichMessage>
        {
            FillPostback = true,
            Editor = EditorTypeEnum.Text,
            Recipient = new Recipient
            {
                Id = conversationId
            },
            Message = new ButtonTemplateMessage
            {
                Text = text,
                Buttons = elements.ToArray()
            }
        };

    }
}
