using static Dapper.SqlMapper;
using Microsoft.Extensions.Logging;
using BotSharp.Core.Infrastructures;
using MySqlConnector;
using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Knowledges.Settings;
using BotSharp.Abstraction.Knowledges.Enums;
using BotSharp.Abstraction.VectorStorage.Models;

namespace BotSharp.Plugin.SqlDriver.Services;

public class DbKnowledgeService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<DbKnowledgeService> _logger;

    public DbKnowledgeService(
        IServiceProvider services,
        ILogger<DbKnowledgeService> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<bool> Import(string provider, string model, string schema)
    {
        var sqlDriverSettings = _services.GetRequiredService<SqlDriverSetting>();
        var knowledgeSettings = _services.GetRequiredService<KnowledgeBaseSettings>();
        var knowledgeService = _services.GetRequiredService<IKnowledgeService>();
        var collectionName = knowledgeSettings.Default.CollectionName ?? KnowledgeCollectionName.BotSharp;

        var tables = new HashSet<string>();
        using var connection = new MySqlConnection(sqlDriverSettings.MySqlConnectionString);

        var sql = $"select table_name from information_schema.tables where table_schema = @tableSchema";
        var results = connection.Query(sql, new
        {
            tableSchema = schema 
        });

        foreach (var item in results)
        {
            if (item == null) continue;

            tables.Add(item.TABLE_NAME);
        }

        foreach (var table in tables)
        {
            try
            {
                _logger.LogInformation($"Start processing table {table}\r\n");

                var ddl = GetTableStructure(table);
                if (string.IsNullOrEmpty(ddl)) continue;

                var prompt = await GetPrompt(ddl);
                var response = await GetAiResponse(prompt, provider, model);
                var knowledges = response.Content.JsonArrayContent<ExtractedKnowledge>();

                if (knowledges.IsNullOrEmpty())
                {
                    _logger.LogInformation($"No knowledge for table {table}");
                    continue;
                }

                foreach (var item in knowledges)
                {
                    await knowledgeService.CreateVectorCollectionData(collectionName, new VectorCreateModel
                    {
                        Text = item.Question,
                        Payload = new Dictionary<string, string>
                        {
                            { KnowledgePayloadName.Answer, item.Answer }
                        }
                    });

                    _logger.LogInformation($"Knowledge {table} is saved =>\r\nQuestion: {item.Question}\r\nAnswer: {item.Answer}\r\n");
                }
            }
            catch (Exception ex)
            {
                var note = $"Error processing table {table}: {ex.Message}\r\n{ex.InnerException}";
                _logger.LogWarning(note);
            }
        }

        return true;
    }

    private string GetTableStructure(string table)
    {
        var settings = _services.GetRequiredService<SqlDriverSetting>();
        
        var ddl = string.Empty;
        var escapedTableName = MySqlHelper.EscapeString(table);
        var sql = $"SHOW CREATE TABLE `{escapedTableName}`";

        using var connection = new MySqlConnection(settings.MySqlConnectionString);
        connection.Open();
        using var command = new MySqlCommand(sql, connection);
        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            ddl = reader.GetString(1);
        }

        reader.Close();
        command.Dispose();
        connection.Close();
        return ddl;
    }

    private async Task<string> GetPrompt(string content)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var render = _services.GetRequiredService<ITemplateRender>();

        var aiAssistant = await agentService.GetAgent(BuiltInAgentId.AIAssistant);
        var template = aiAssistant.Templates.FirstOrDefault(x => x.Name == "database_knowledge")?.Content ?? string.Empty;

        return render.Render(template, new Dictionary<string, object>
        {
            { "table_structure", content }
        });
    }

    private async Task<RoleDialogModel> GetAiResponse(string prompt, string provider, string model)
    {
        var agent = new Agent
        {
            Id = string.Empty,
            Name = "Db knowledge",
            Instruction = prompt,
        };

        var completion = CompletionProvider.GetChatCompletion(_services, provider, model);
        return await completion.GetChatCompletions(agent, new List<RoleDialogModel>());
    }
}
