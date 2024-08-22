using BotSharp.Abstraction.Routing;
using BotSharp.Core.Infrastructures;
using MySqlConnector;
using static Dapper.SqlMapper;
using BotSharp.Abstraction.Agents.Enums;


namespace BotSharp.Plugin.Planner.Functions;
public class AddDatabaseKnowledgeFn : IFunctionCallback
{
    public string Name => "add_database_knowledge";
    private readonly IServiceProvider _services;
    private object aiAssistant;

    public AddDatabaseKnowledgeFn(IServiceProvider services)
    {
        _services = services;
    }
    public async Task<bool> Execute(RoleDialogModel message)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var sqlDriver = _services.GetRequiredService<SqlDriverService>();
        var fn = _services.GetRequiredService<IRoutingService>();
        var settings = _services.GetRequiredService<SqlDriverSetting>();
        using var connection = new MySqlConnection(settings.MySqlConnectionString);
        var dictionary = new Dictionary<string, object>();

        List<string> allTables = new List<string>();
        //var sql = $"select table_name from information_schema.tables where table_schema='GSMPMetaData' and (table_name like 'affiliate_%' or table_name like 'client_%' or table_name like 'data_%' or table_name like 'sms_%') limit 10";
        var sql = $"select * from GSMPMetaData.joanna_knowledgebase where id>1057;";
        //var sql = $"select table_name from information_schema.tables where table_schema='GSMPMetaData' and table_name like 'data_%'";
        //var sql = $"select table_name from information_schema.tables where table_schema='GSMPMetaData' and (table_name like 'client_%Reactive%' or table_name like 'data_%' or table_name like 'sms_%');";
        var result = connection.Query(sql: sql,dictionary);
        //var result2 = new string[] { "client_ServiceCategory", "client_ServiceCode", "client_ServiceCodeNTE", "client_ServiceType" };
        foreach (var item in result)
        {
            allTables.Add(item.TABLE_NAME);
        }
        message.Data = allTables.Distinct().ToList();

        var currentAgent = await agentService.LoadAgent(message.CurrentAgentId);
        var note = "";
        foreach (var item in allTables)
        {
            message.Data = new List<string> { item };

            await fn.InvokeFunction("get_table_definition", message);
            var PlanningPrompt = await GetPrompt(message);
            var plannerAgent = new Agent
            {
                Id = "",
                Name = "database_knowledge",
                Instruction = PlanningPrompt,
                TemplateDict = new Dictionary<string, object>(),
                LlmConfig = currentAgent.LlmConfig
            };
            var response = await GetAIResponse(plannerAgent);
            try
            {
                var knowledge = response.Content.JsonArrayContent<ExtractedKnowledge>();
                foreach (var k in knowledge)
                {
                    try
                    {
                        message.FunctionArgs = JsonSerializer.Serialize(new ExtractedKnowledge
                        {
                            Question = k.Question,
                            Answer = k.Answer
                        });
                        await fn.InvokeFunction("memorize_knowledge", message);
                        message.SecondaryContent += $"Table: {item}, Question:{k.Question}, {message.Content} \r\n";
                    }
                    catch (Exception e)
                    {
                        note += $"Error processing table {item}: {e.Message}\r\n{e.InnerException}";
                    }
                }
            }
            catch (Exception e)
            {
                note += $"Error processing table {item}: {e.Message}\r\n{e.InnerException}";
            }
        }
        //message.Data = allTables.Distinct().ToList();
        return true;
    }
    private async Task<RoleDialogModel> GetAIResponse(Agent plannerAgent)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        var wholeDialogs = conv.GetDialogHistory();
        var completion = CompletionProvider.GetChatCompletion(_services,
            provider: plannerAgent.LlmConfig.Provider,
            model: plannerAgent.LlmConfig.Model);
        //wholeDialogs.Last().Content += "\n\n=========\n\nthe summarized question/answer should:\n1. help user to identify the location of tables to find further information\n2. identify the table structure and data relationship based on the task description\n3. summarize all the table to table relationship  information based on the FOREIGN KEY, and include both table in the answer";

        return await completion.GetChatCompletions(plannerAgent, wholeDialogs);
    }
    private async Task<string> GetPrompt(RoleDialogModel message)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var aiAssistant = await agentService.GetAgent(BuiltInAgentId.AIAssistant);
        var render = _services.GetRequiredService<ITemplateRender>();
        var template = aiAssistant.Templates.First(x => x.Name == "database_knowledge").Content;
        //var responseFormat = JsonSerializer.Serialize(new JsonDocument[] { JsonDocument.Parse("{}") });
        var responseFormat = JsonSerializer.Serialize(new ExtractedKnowledge
        {
            Question = "question",
            Answer = "answer"
        });

        return render.Render(template, new Dictionary<string, object>
        {
            { "table_structure", message.Content }
        });
    }
}
