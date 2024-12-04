using BotSharp.Core.Infrastructures;
using MySqlConnector;
using static Dapper.SqlMapper;

namespace BotSharp.Plugin.SqlDriver.UtilFunctions;

public class VerifyDictionaryTerm : IFunctionCallback
{
    public string Name => "util-db-verify_dictionary_term";
    public string Indication => "Verifying dictionary term";


    private readonly IServiceProvider _services;

    public VerifyDictionaryTerm(IServiceProvider services)
    {
        _services = services;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<LookupDictionary>(message.FunctionArgs);

        // get table DDL
        var fn = _services.GetRequiredService<IRoutingService>();
        var msgCopy = RoleDialogModel.From(message);
        await fn.InvokeFunction("sql_table_definition", msgCopy);

        // refine SQL
        var agentService = _services.GetRequiredService<IAgentService>();
        var currentAgent = await agentService.LoadAgent(message.CurrentAgentId);
        var dictionarySqlPrompt = await GetDictionarySQLPrompt(args.SqlStatement, msgCopy.Content);
        var agent = new Agent
        {
            Id = message.CurrentAgentId ?? string.Empty,
            Name = "sqlDriver_DictionarySearch",
            Instruction = dictionarySqlPrompt,
            TemplateDict = new Dictionary<string, object>(),
            LlmConfig = currentAgent.LlmConfig
        };

        var response = await GetAiResponse(agent);
        args = response.Content.JsonContent<LookupDictionary>();

        // check if need to instantely
        IEnumerable<dynamic>? result = null;
        if (!string.IsNullOrWhiteSpace(args.SqlStatement))
        {
            var settings = _services.GetRequiredService<SqlDriverSetting>();
            using var connection = new MySqlConnection(settings.MySqlExecutionConnectionString);
            result = connection.Query(args.SqlStatement);
        }

        if (result.IsNullOrEmpty())
        {
            message.Content = "Record not found";
        }
        else
        {
            message.Content = JsonSerializer.Serialize(result);
        }

        var states = _services.GetRequiredService<IConversationStateService>();
        var dictionaryItems = states.GetState("dictionary_items", "");
        var newItem = BuildDictionaryItem(args.Table, args.Reason, message.Content);
        dictionaryItems += !string.IsNullOrWhiteSpace(newItem) ? $"\r\n{newItem}\r\n" : string.Empty;
        states.SetState("dictionary_items", dictionaryItems);

        return true;
    }

    private async Task<string> GetDictionarySQLPrompt(string originalSql, string tableStructure)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var render = _services.GetRequiredService<ITemplateRender>();
        var knowledgeHooks = _services.GetServices<IKnowledgeHook>();

        var agent = await agentService.GetAgent(BuiltInAgentId.SqlDriver);
        var template = agent.Templates.FirstOrDefault(x => x.Name == "database.dictionary.sql")?.Content ?? string.Empty;
        var responseFormat = JsonSerializer.Serialize(new LookupDictionary{ });

        return render.Render(template, new Dictionary<string, object>
        {
            { "original_sql", originalSql },
            { "table_structure", tableStructure },
            { "response_format", responseFormat }
        });
    }

    private async Task<RoleDialogModel> GetAiResponse(Agent agent)
    {
        var text = "Check and correct the SQL statement.";
        var message = new RoleDialogModel(AgentRole.User, text);

        var completion = CompletionProvider.GetChatCompletion(_services,
            provider: agent.LlmConfig.Provider,
            model: agent.LlmConfig.Model);

        return await completion.GetChatCompletions(agent, new List<RoleDialogModel> { message });
    }

    private string BuildDictionaryItem(string? table, string? reason, string? result)
    {
        var res = string.Empty;
        if (!string.IsNullOrWhiteSpace(table))
        {
            res += $"Table: {table}";
        }

        if (!string.IsNullOrWhiteSpace(reason))
        {
            if (!string.IsNullOrWhiteSpace(res))
            {
                res += "\r\n";
            }
            res += $"Reason: {reason}";
        }

        if (!string.IsNullOrWhiteSpace(result))
        {
            if (!string.IsNullOrWhiteSpace(res))
            {
                res += "\r\n";
            }
            res += $"Result: {result}";
        }

        return res;
    }
}
