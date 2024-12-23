using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Loggers;
using Microsoft.Extensions.Logging;
using Mscc.GenerativeAI;

namespace BotSharp.Plugin.GoogleAi.Providers.Chat;

public class GeminiChatCompletionProvider : IChatCompletion
{
    private readonly IServiceProvider _services;
    private readonly ILogger<GeminiChatCompletionProvider> _logger;

    private string _model;

    public string Provider => "google-gemini";

    public GeminiChatCompletionProvider(
        IServiceProvider services,
        ILogger<GeminiChatCompletionProvider> logger)
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

        var client = ProviderHelper.GetGeminiClient(_services);
        var aiModel = client.GenerativeModel(_model);
        var (prompt, request) = PrepareOptions(aiModel, agent, conversations);

        var response = await aiModel.GenerateContent(request);
        var candidate = response.Candidates.First();
        var part = candidate.Content?.Parts?.FirstOrDefault();
        var text = part?.Text ?? string.Empty;

        RoleDialogModel responseMessage;
        if (part?.FunctionCall != null)
        {
            responseMessage = new RoleDialogModel(AgentRole.Function, text)
            {
                CurrentAgentId = agent.Id,
                MessageId = conversations.LastOrDefault()?.MessageId ?? string.Empty,
                ToolCallId = part.FunctionCall.Name,
                FunctionName = part.FunctionCall.Name,
                FunctionArgs = part.FunctionCall.Args?.ToString()
            };
        }
        else
        {
            responseMessage = new RoleDialogModel(AgentRole.Assistant, text)
            {
                CurrentAgentId = agent.Id,
                MessageId = conversations.LastOrDefault()?.MessageId ?? string.Empty,
            };
        }

        // After chat completion hook
        foreach (var hook in contentHooks)
        {
            await hook.AfterGenerated(responseMessage, new TokenStatsModel
            {
                Prompt = prompt,
                Provider = Provider,
                Model = _model
            });
        }

        return responseMessage;
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

    private (string, GenerateContentRequest) PrepareOptions(GenerativeModel aiModel, Agent agent, List<RoleDialogModel> conversations)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var googleSettings = _services.GetRequiredService<GoogleAiSettings>();

        // Add settings
        aiModel.UseGoogleSearch = googleSettings.Gemini.UseGoogleSearch;
        aiModel.UseGrounding = googleSettings.Gemini.UseGrounding;

        // Assembly messages
        var contents = new List<Content>();
        var tools = new List<Tool>();
        var funcDeclarations = new List<FunctionDeclaration>();

        var systemPrompts = new List<string>();
        if (!string.IsNullOrEmpty(agent.Instruction))
        {
            var instruction = agentService.RenderedInstruction(agent);
            contents.Add(new Content(instruction)
            {
                Role = AgentRole.User
            });

            systemPrompts.Add(instruction);
        }

        var funcPrompts = new List<string>();
        foreach (var function in agent.Functions)
        {
            if (!agentService.RenderFunction(agent, function)) continue;

            var def = agentService.RenderFunctionProperty(agent, function);

            funcDeclarations.Add(new FunctionDeclaration
            {
                Name = function.Name,
                Description = function.Description,
                Parameters = new()
                {
                    Type = ParameterType.Object,
                    Properties = def.Properties,
                    Required = def.Required
                }
            });

            funcPrompts.Add($"{function.Name}: {function.Description} {def}");
        }

        if (!funcDeclarations.IsNullOrEmpty())
        {
            tools.Add(new Tool { FunctionDeclarations = funcDeclarations });
        }

        var convPrompts = new List<string>();
        foreach (var message in conversations)
        {
            if (message.Role == AgentRole.Function)
            {
                contents.Add(new Content(message.Content)
                {
                    Role = AgentRole.Function,
                    Parts = new()
                    {
                        new FunctionCall
                        {
                            Name = message.FunctionName,
                            Args = JsonSerializer.Deserialize<object>(message.FunctionArgs ?? "{}")
                        }
                    }
                });

                convPrompts.Add($"{AgentRole.Assistant}: Call function {message.FunctionName}({message.FunctionArgs})");
            }
            else if (message.Role == AgentRole.User)
            {
                var text = !string.IsNullOrWhiteSpace(message.Payload) ? message.Payload : message.Content;
                contents.Add(new Content(text)
                {
                    Role = AgentRole.User
                });
                convPrompts.Add($"{AgentRole.User}: {text}");
            }
            else if (message.Role == AgentRole.Assistant)
            {
                contents.Add(new Content(message.Content)
                {
                    Role = AgentRole.Model
                });
                convPrompts.Add($"{AgentRole.Assistant}: {message.Content}");
            }
        }

        var request = new GenerateContentRequest
        {
            Contents = contents,
            Tools = tools
        };

        var prompt = GetPrompt(systemPrompts, funcPrompts, convPrompts);
        return (prompt, request);
    }

    private string GetPrompt(IEnumerable<string> systemPrompts, IEnumerable<string> funcPrompts, IEnumerable<string> convPrompts)
    {
        var prompt = string.Empty;

        prompt = string.Join("\r\n\r\n", systemPrompts);

        if (!funcPrompts.IsNullOrEmpty())
        {
            prompt += "\r\n\r\n[FUNCTIONS]\r\n";
            prompt += string.Join("\r\n", funcPrompts);
        }

        if (!convPrompts.IsNullOrEmpty())
        {
            prompt += "\r\n\r\n[CONVERSATION]\r\n";
            prompt += string.Join("\r\n", convPrompts);
        }

        return prompt;
    }
}
