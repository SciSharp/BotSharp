using Amazon.Runtime.Internal.Transform;
using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Core.Infrastructures;
using BotSharp.Plugin.SqlDriver.Models;
using MySqlConnector;
using static Dapper.SqlMapper;

namespace BotSharp.Plugin.SqlDriver.Functions;

public class LookupDictionaryFn : IFunctionCallback
{
    public string Name => "lookup_dictionary";
    private readonly IServiceProvider _services;

    public LookupDictionaryFn(IServiceProvider services)
    {
        _services = services;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<LookupDictionary>(message.FunctionArgs);

        var settings = _services.GetRequiredService<SqlDriverSetting>();
        using var connection = new MySqlConnection(settings.MySqlConnectionString);
        var dictionary = new Dictionary<string, object>();
        var results = connection.Query($"SELECT * FROM {args.Table} LIMIT 10");
        var items = new List<string>();
        foreach(var item in results)
        {
            items.Add(JsonSerializer.Serialize(item));
        }

        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.LoadAgent(message.CurrentAgentId);
        var prompt = GetPrompt(agent, items, args.Keyword);

        // Ask LLM which one is the best
        var llmProviderService = _services.GetRequiredService<ILlmProviderService>();
        var model = llmProviderService.GetProviderModel("azure-openai", "gpt-35-turbo");

        // chat completion
        var completion = CompletionProvider.GetChatCompletion(_services,
            provider: "azure-openai",
            model: model.Name);

        var conversations = new List<RoleDialogModel>
        {
            new RoleDialogModel(AgentRole.User, prompt)
            {
                CurrentAgentId = message.CurrentAgentId,
                MessageId = message.MessageId,
            } 
        };

        var response = await completion.GetChatCompletions(new Agent
        {
            Id = message.CurrentAgentId,
            Instruction = ""
        }, conversations);

        message.Content = response.Content;

        return true;
    }

    private string GetPrompt(Agent agent, List<string> task, string keyword)
    {
        var template = agent.Templates.First(x => x.Name == "lookup_dictionary").Content;

        var render = _services.GetRequiredService<ITemplateRender>();
        return render.Render(template, new Dictionary<string, object>
        {
            { "items", task },
            { "keyword", keyword }
        });
    }
}
