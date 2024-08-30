using BotSharp.Abstraction.Routing;
using BotSharp.Core.Infrastructures;
using MySqlConnector;
using static Dapper.SqlMapper;
using BotSharp.Abstraction.Agents.Enums;
using Microsoft.Extensions.Logging;

namespace BotSharp.Plugin.Planner.Functions;

public class AddDatabaseKnowledgeFn : IFunctionCallback
{
    public string Name => "add_database_knowledge";

    private readonly IServiceProvider _services;
    private readonly ILogger<AddDatabaseKnowledgeFn> _logger;

    public AddDatabaseKnowledgeFn(
        IServiceProvider services,
        ILogger<AddDatabaseKnowledgeFn> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var sqlDriver = _services.GetRequiredService<SqlDriverService>();
        var fn = _services.GetRequiredService<IRoutingService>();
        var settings = _services.GetRequiredService<SqlDriverSetting>();


        var allTables = new HashSet<string>();
        using var connection = new MySqlConnection(settings.MySqlConnectionString);

        var sql = $"select table_name from information_schema.tables;";
        var results = connection.Query(sql, new Dictionary<string, object>());

        foreach (var item in results)
        {
            if (item == null) continue;

            allTables.Add(item.TABLE_NAME);
        }
        message.Data = allTables.ToList();

        var currentAgent = await agentService.LoadAgent(message.CurrentAgentId);
        var errorNote = string.Empty;

        foreach (var table in allTables)
        {
            message.Data = new List<string> { table };

            await fn.InvokeFunction("get_table_definition", message);
            var planningPrompt = await GetPrompt(message);
            var plannerAgent = new Agent
            {
                Id = string.Empty,
                Name = "Database Knowledge",
                Instruction = planningPrompt,
                LlmConfig = currentAgent.LlmConfig
            };
            
            try
            {
                var response = await GetAiResponse(plannerAgent);
                var knowledges = response.Content.JsonArrayContent<ExtractedKnowledge>();
                foreach (var k in knowledges)
                {
                    try
                    {
                        message.FunctionArgs = JsonSerializer.Serialize(new ExtractedKnowledge
                        {
                            Question = k.Question,
                            Answer = k.Answer
                        });
                        await fn.InvokeFunction("memorize_knowledge", message);
                        message.SecondaryContent += $"Table: {table}, Question: {k.Question}, {message.Content}\r\n";
                    }
                    catch (Exception e)
                    {
                        var note = $"Error processing table {table}: {e.Message}\r\n{e.InnerException}";
                        errorNote += note;
                        _logger.LogWarning(note);
                    }
                }
            }
            catch (Exception e)
            {
                errorNote += $"Error processing table {table}: {e.Message}\r\n{e.InnerException}\r\n";
                _logger.LogWarning(errorNote);
            }
        }
        return true;
    }

    private async Task<RoleDialogModel> GetAiResponse(Agent plannerAgent)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        var wholeDialogs = conv.GetDialogHistory();

        var completion = CompletionProvider.GetChatCompletion(_services, 
            provider: plannerAgent.LlmConfig.Provider,
            model: plannerAgent.LlmConfig.Model);

        return await completion.GetChatCompletions(plannerAgent, wholeDialogs);
    }

    private async Task<string> GetPrompt(RoleDialogModel message)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var render = _services.GetRequiredService<ITemplateRender>();

        var aiAssistant = await agentService.GetAgent(BuiltInAgentId.AIAssistant);
        var template = aiAssistant.Templates.FirstOrDefault(x => x.Name == "database_knowledge")?.Content ?? string.Empty;

        return render.Render(template, new Dictionary<string, object>
        {
            { "table_structure", message.Content }
        });
    }
}
