using BotSharp.Abstraction.MLTasks;

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

        await HookEmitter.Emit<IText2SqlHook>(_services, async (hook) =>
        {
            await hook.SqlGenerated(msg);
        }, msg.CurrentAgentId);

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

        // Invoke "execute_sql"
        var executionMsg = new RoleDialogModel(AgentRole.Function, "execute sql and format the result")
        {
            FunctionArgs = JsonSerializer.Serialize(new ExecuteQueryArgs
            {
                SqlStatements = [msg.Content]
            })
        };
        await routing.InvokeFunction("execute_sql", executionMsg);
        msg.Content = $"The SQL query has been reviewed and executed, the formatted result is: \r\n{executionMsg.Content}";
    }

    public async Task OnPlanningCompleted(string planner, RoleDialogModel msg)
    {

    }

    public async Task<string> GetSummaryAdditionalRequirements(string planner, RoleDialogModel message)
    {
        var settings = _services.GetRequiredService<SqlDriverSetting>();
        var sqlHook = _services.GetRequiredService<IText2SqlHook>();
        var agentService = _services.GetRequiredService<IAgentService>();

        var dbType = sqlHook.GetDatabaseType(message);
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
