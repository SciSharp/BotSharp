using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Routing;
using BotSharp.Plugin.GoogleAI.Settings;
using LLMSharp.Google.Palm;
using Microsoft.Extensions.Logging;
using System.Diagnostics.Metrics;
using static System.Net.Mime.MediaTypeNames;

namespace BotSharp.Plugin.GoogleAI.Providers;

public class ChatCompletionProvider : IChatCompletion
{
    public string Provider => "google-ai";
    private readonly IServiceProvider _services;
    private readonly GoogleAiSettings _settings;
    private readonly ILogger _logger;
    private string _model;

    public ChatCompletionProvider(IServiceProvider services, 
        GoogleAiSettings settings,
        ILogger<ChatCompletionProvider> logger)
    {
        _services = services;
        _settings = settings;
        _logger = logger;
    }

    public RoleDialogModel GetChatCompletions(Agent agent, List<RoleDialogModel> conversations)
    {
        var hooks = _services.GetServices<IContentGeneratingHook>().ToList();

        // Before chat completion hook
        Task.WaitAll(hooks.Select(hook =>
            hook.BeforeGenerating(agent, conversations)).ToArray());

        var client = new GooglePalmClient(apiKey: _settings.PaLM.ApiKey);

        var (prompt, messages) = PrepareOptions(agent, conversations);

        RoleDialogModel msg;
        
        if (messages == null)
        {
            // use text completion
            var response = client.GenerateTextAsync(prompt, null).Result;

            var message = response.Candidates.First();

            // check if returns function calling
            var llmResponse = message.Output.JsonContent<FunctionCallingResponse>();

            msg = new RoleDialogModel(llmResponse.Role, llmResponse.Content)
            {
                CurrentAgentId = agent.Id,
                FunctionName = llmResponse.FunctionName,
                FunctionArgs = JsonSerializer.Serialize(llmResponse.Args)
            };
        }
        else
        {
            var response = client.ChatAsync(messages, context: prompt, examples: null, options: null).Result;

            var message = response.Candidates.First();

            // check if returns function calling
            var llmResponse = message.Content.JsonContent<FunctionCallingResponse>();

            msg = new RoleDialogModel(llmResponse.Role, llmResponse.Content ?? message.Content)
            {
                CurrentAgentId = agent.Id
            };
        }

        // After chat completion hook
        Task.WaitAll(hooks.Select(hook =>
            hook.AfterGenerated(msg, new TokenStatsModel
            {
                Model = _model
            })).ToArray());

        return msg;
    }

    private (string, List<PalmChatMessage>) PrepareOptions(Agent agent, List<RoleDialogModel> conversations)
    {
        var prompt = "";

        var agentService = _services.GetRequiredService<IAgentService>();

        if (!string.IsNullOrEmpty(agent.Instruction))
        {
            prompt += agentService.RenderedInstruction(agent);
        }

        var routing = _services.GetRequiredService<IRoutingService>();
        var router = routing.Router;

        if (agent.Functions != null && agent.Functions.Count > 0)
        {
            prompt += "\r\n\r\n[Functions] defined in JSON Schema:\r\n";
            prompt += JsonSerializer.Serialize(agent.Functions, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

            prompt += "\r\n\r\n[Conversations]\r\n";
            foreach (var dialog in conversations)
            {
                prompt += dialog.Role == AgentRole.Function ?
                    $"{dialog.Role}: {dialog.FunctionName} => {dialog.Content}\r\n" :
                    $"{dialog.Role}: {dialog.Content}\r\n";
            }

            prompt += "\r\n\r\n" + router.Templates.FirstOrDefault(x => x.Name == "response_with_function").Content;

            return (prompt, null);
        }

        var messages = conversations.Select(c => new PalmChatMessage(c.Content, c.Role == AgentRole.User ? "user" : "AI"))
            .ToList();

        return (prompt, messages);
    }

    public Task<bool> GetChatCompletionsAsync(Agent agent, List<RoleDialogModel> conversations, Func<RoleDialogModel, Task> onMessageReceived, Func<RoleDialogModel, Task> onFunctionExecuting)
    {
        throw new NotImplementedException();
    }

    public Task<bool> GetChatCompletionsStreamingAsync(Agent agent, List<RoleDialogModel> conversations, Func<RoleDialogModel, Task> onMessageReceived)
    {
        throw new NotImplementedException();
    }

    public void SetModelName(string model)
    {
        _model = model;
    }
}
