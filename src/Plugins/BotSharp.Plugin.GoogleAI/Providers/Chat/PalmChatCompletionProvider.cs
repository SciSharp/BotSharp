using BotSharp.Abstraction.Routing;
using LLMSharp.Google.Palm;
using LLMSharp.Google.Palm.DiscussService;
using BotSharp.Abstraction.Hooks;

namespace BotSharp.Plugin.GoogleAi.Providers.Chat;

[Obsolete]
public class PalmChatCompletionProvider : IChatCompletion
{
    private readonly IServiceProvider _services;
    private readonly ILogger<PalmChatCompletionProvider> _logger;
    private List<string> renderedInstructions = [];

    private string _model;

    public string Provider => "google-palm";
    public string Model => _model;
   
    public PalmChatCompletionProvider(
        IServiceProvider services,
        ILogger<PalmChatCompletionProvider> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<RoleDialogModel> GetChatCompletions(Agent agent, List<RoleDialogModel> conversations)
    {
        var contentHooks = _services.GetHooks<IContentGeneratingHook>(agent.Id);

        // Before chat completion hook
        foreach (var hook in contentHooks)
        {
            await hook.BeforeGenerating(agent, conversations);
        }

        var client = ProviderHelper.GetPalmClient(Provider, _model, _services);
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
                FunctionArgs = JsonSerializer.Serialize(llmResponse.Args),
                RenderedInstruction = string.Join("\r\n", renderedInstructions)
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
                CurrentAgentId = agent.Id,
                RenderedInstruction = string.Join("\r\n", renderedInstructions)
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
        var agentService = _services.GetRequiredService<IAgentService>();
        var routing = _services.GetRequiredService<IRoutingService>();
        var router = routing.Router;

        // Prepare instruction and functions
        var renderData = agentService.CollectRenderData(agent);
        var (prompt, functions) = agentService.PrepareInstructionAndFunctions(agent, renderData);
        if (!string.IsNullOrWhiteSpace(prompt))
        {
            renderedInstructions.Add(prompt);
        }

        var messages = conversations.Select(c => new PalmChatMessage(c.RoleContent, c.Role == AgentRole.User ? "user" : "AI"))
            .ToList();

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
                    $"{dialog.Role}: {dialog.FunctionName} => {dialog.RoleContent}\r\n" :
                    $"{dialog.Role}: {dialog.RoleContent}\r\n";
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

    public Task<RoleDialogModel> GetChatCompletionsStreamingAsync(Agent agent, List<RoleDialogModel> conversations)
    {
        throw new NotImplementedException();
    }

    public void SetModelName(string model)
    {
        _model = model;
    }
}
