using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Loggers;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Routing;
using LLMSharp.Google.Palm;
using LLMSharp.Google.Palm.DiscussService;
using Microsoft.Extensions.Logging;

namespace BotSharp.Plugin.GoogleAi.Providers.Chat;

public class PalmChatCompletionProvider : IChatCompletion
{
    private readonly IServiceProvider _services;
    private readonly ILogger<PalmChatCompletionProvider> _logger;

    private string _model;

    public string Provider => "google-ai";

    public PalmChatCompletionProvider(
        IServiceProvider services,
        ILogger<PalmChatCompletionProvider> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<RoleDialogModel> GetChatCompletions(Agent agent, List<RoleDialogModel> conversations)
    {
        var contentHooks = _services.GetServices<IContentGeneratingHook>().ToList();

        // Before chat completion hook
        foreach (var hook in contentHooks)
        {
            await hook.BeforeGenerating(agent, conversations);
        }

        var client = ProviderHelper.GetPalmClient(_services);
        var (prompt, messages, hasFunctions) = PrepareOptions(agent, conversations);

        RoleDialogModel msg;

        if (hasFunctions)
        {
            // use text completion
            // var response = client.GenerateTextAsync(prompt, null).Result;
            var response = await client.ChatAsync(new PalmChatCompletionRequest
            {
                Context = prompt,
                Messages = messages,
                Temperature = 0.1f
            });

            var message = response.Candidates.First();

            // check if returns function calling
            var llmResponse = message.Content.JsonContent<FunctionCallingResponse>();

            msg = new RoleDialogModel(llmResponse.Role, llmResponse.Content)
            {
                CurrentAgentId = agent.Id,
                FunctionName = llmResponse.FunctionName,
                FunctionArgs = JsonSerializer.Serialize(llmResponse.Args)
            };
        }
        else
        {
            var response = await client.ChatAsync(messages, context: prompt, examples: null, options: null);

            var message = response.Candidates.First();

            // check if returns function calling
            var llmResponse = message.Content.JsonContent<FunctionCallingResponse>();

            msg = new RoleDialogModel(llmResponse.Role, llmResponse.Content ?? message.Content)
            {
                CurrentAgentId = agent.Id
            };
        }

        // After chat completion hook
        foreach (var hook in contentHooks)
        {
            await hook.AfterGenerated(msg, new TokenStatsModel
            {
                Prompt = prompt,
                Provider = Provider,
                Model = _model
            });
        }

        return msg;
    }

    private (string, List<PalmChatMessage>, bool) PrepareOptions(Agent agent, List<RoleDialogModel> conversations)
    {
        var prompt = "";

        var agentService = _services.GetRequiredService<IAgentService>();

        if (!string.IsNullOrEmpty(agent.Instruction) || !agent.SecondaryInstructions.IsNullOrEmpty())
        {
            prompt += agentService.RenderedInstruction(agent);
        }

        var routing = _services.GetRequiredService<IRoutingService>();
        var router = routing.Router;

        var messages = conversations.Select(c => new PalmChatMessage(c.Content, c.Role == AgentRole.User ? "user" : "AI"))
            .ToList();

        var functions = agent.Functions.Concat(agent.SecondaryFunctions ?? []);
        if (!functions.IsNullOrEmpty())
        {
            prompt += "\r\n\r\n[Functions] defined in JSON Schema:\r\n";
            prompt += JsonSerializer.Serialize(functions, new JsonSerializerOptions
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

            return (prompt, new List<PalmChatMessage>
            {
                new PalmChatMessage("Which function should be used for the next step based on latest user or function response, output your response in JSON:", AgentRole.User),
            }, true);
        }

        return (prompt, messages, false);
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
