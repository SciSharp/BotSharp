using Azure;
using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.Routing;
using BotSharp.Core.Agents.Services;
using BotSharp.Core.Infrastructures;
using BotSharp.Plugin.SqlDriver.Models;
using MySqlConnector;
using System.Text.RegularExpressions;
using static Dapper.SqlMapper;
using static System.Net.Mime.MediaTypeNames;

namespace BotSharp.Plugin.SqlDriver.Functions;

public class LookupDictionaryFn : IFunctionCallback
{
    public string Name => "sql_dictionary_lookup";
    private readonly IServiceProvider _services;

    public LookupDictionaryFn(IServiceProvider services)
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
        var plannerAgent = new Agent
        {
            Id = string.Empty,
            Name = "sqlDriver_DictionarySearch",
            Instruction = dictionarySqlPrompt,
            TemplateDict = new Dictionary<string, object>(),
            LlmConfig = currentAgent.LlmConfig
        };
        var response = await GetAiResponse(plannerAgent);
        args = JsonSerializer.Deserialize<LookupDictionary>(response.Content);

        // check if need to instantely
        var settings = _services.GetRequiredService<SqlDriverSetting>();
        using var connection = new MySqlConnection(settings.MySqlExecutionConnectionString);
        var result = connection.Query(args.SqlStatement);

        if (result == null)
        {
            message.Content = "Record not found";
        }
        else
        {
            message.Content = JsonSerializer.Serialize(result);
        }
        var states = _services.GetRequiredService<IConversationStateService>();
        var dictionaryItems = states.GetState("dictionary_items", "");
        dictionaryItems += "\r\n\r\n" + args.Table + ":\r\n" + args.Reason + ":\r\n" + message.Content + "\r\n";
        states.SetState("dictionary_items", dictionaryItems);

        return true;
    }
    private async Task<string> GetDictionarySQLPrompt(string originalSql, string tableStructure)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var render = _services.GetRequiredService<ITemplateRender>();
        var knowledgeHooks = _services.GetServices<IKnowledgeHook>();

        var agent = await agentService.GetAgent(BuiltInAgentId.Planner);
        var template = agent.Templates.FirstOrDefault(x => x.Name == "database.dictionary.sql")?.Content ?? string.Empty;
        var responseFormat = JsonSerializer.Serialize(new LookupDictionary{ });

        return render.Render(template, new Dictionary<string, object>
        {
            { "original_sql", originalSql },
            { "table_structure", tableStructure },
            { "response_format", responseFormat }
        });
    }
    private async Task<RoleDialogModel> GetAiResponse(Agent plannerAgent)
    {
        var text = "Check and correct the SQL statement.";
        var message = new RoleDialogModel(AgentRole.User, text);

        var completion = CompletionProvider.GetChatCompletion(_services,
            provider: plannerAgent.LlmConfig.Provider,
            model: plannerAgent.LlmConfig.Model);

        return await completion.GetChatCompletions(plannerAgent, new List<RoleDialogModel> { message });
    }
}
