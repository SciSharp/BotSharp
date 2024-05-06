using Anthropic.SDK.Common;
using BotSharp.Abstraction.MLTasks.Settings;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace BotSharp.Plugin.AnthropicAI.Providers;

public class ChatCompletionProvider : IChatCompletion
{
    public string Provider => "anthropic";

    protected readonly AnthropicSettings _settings;
    protected readonly IServiceProvider _services;
    protected readonly ILogger _logger;

    protected string _model;

    public ChatCompletionProvider(AnthropicSettings settings,
        ILogger<ChatCompletionProvider> logger,
        IServiceProvider services)
    {
        _settings = settings;
        _logger = logger;
        _services = services;
    }

    public async Task<RoleDialogModel> GetChatCompletions(Agent agent, List<RoleDialogModel> conversations)
    {
        var contentHooks = _services.GetServices<IContentGeneratingHook>().ToList();

        // Before chat completion hook
        foreach (var hook in contentHooks)
        {
            await hook.BeforeGenerating(agent, conversations);
        }

        var settingsService = _services.GetRequiredService<ILlmProviderService>();
        var settings = settingsService.GetSetting("anthropic", agent.LlmConfig?.Model ?? "claude-3-haiku");

        var client = new AnthropicClient(new APIAuthentication(settings.ApiKey));
        var (prompt, parameters) = PrepareOptions(agent, conversations, settings);

        var response = await client.Messages.GetClaudeMessageAsync(parameters);

        RoleDialogModel responseMessage;

        if (response.StopReason == "tool_use")
        {
            var toolResult = response.Content.OfType<ToolUseContent>().First();

            responseMessage = new RoleDialogModel(AgentRole.Function, response.FirstMessage?.Text)
            {
                CurrentAgentId = agent.Id,
                MessageId = conversations.Last().MessageId,
                ToolCallId = toolResult.Id,
                FunctionName = toolResult.Name,
                FunctionArgs = JsonSerializer.Serialize(toolResult.Input)
            };
        }
        else
        {
            var message = response.FirstMessage;
            responseMessage = new RoleDialogModel(AgentRole.Assistant, message.Text)
            {
                CurrentAgentId = agent.Id,
                MessageId = conversations.Last().MessageId
            };
        }

        // After chat completion hook
        foreach (var hook in contentHooks)
        {
            await hook.AfterGenerated(responseMessage, new TokenStatsModel
            {
                Prompt = prompt,
                Provider = Provider,
                Model = _model,
                PromptCount = response.Usage.InputTokens,
                CompletionCount = response.Usage.OutputTokens
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

    private (string, MessageParameters) PrepareOptions(Agent agent, List<RoleDialogModel> conversations, LlmModelSetting settings)
    {
        var instruction = "";

        var agentService = _services.GetRequiredService<IAgentService>();

        if (!string.IsNullOrEmpty(agent.Instruction))
        {
            instruction += agentService.RenderedInstruction(agent);
        }

        /*var routing = _services.GetRequiredService<IRoutingService>();
        var router = routing.Router;

        var render = _services.GetRequiredService<ITemplateRender>();
        var template = router.Templates.FirstOrDefault(x => x.Name == "response_with_function").Content;

        var response_with_function = render.Render(template, new Dictionary<string, object>
        {
            { "functions", agent.Functions }
        });

        prompt += "\r\n\r\n" + response_with_function;*/

        var messages = new List<Message>();
        foreach (var conv in conversations)
        {
            if (conv.Role == AgentRole.User)
            {
                messages.Add(new Message(RoleType.User, conv.Content));
            }
            else if (conv.Role == AgentRole.Assistant)
            {
                messages.Add(new Message(RoleType.Assistant, conv.Content));
            }
            else if (conv.Role == AgentRole.Function)
            {
                messages.Add(new Message
                {
                    Role = RoleType.Assistant,
                    Content = new List<ContentBase>
                    {
                        new ToolUseContent()
                        {
                            Id = conv.ToolCallId,
                            Name = conv.FunctionName,
                            Input = JsonNode.Parse(conv.FunctionArgs ?? "{}")
                        }
                    }
                });

                messages.Add(new Message()
                {
                    Role = RoleType.User,
                    Content = new List<ContentBase>
                    {
                        new ToolResultContent()
                        {
                            ToolUseId = conv.ToolCallId,
                            Content = conv.Content
                        }
                    }
                });
            }
        }

        var parameters = new MessageParameters()
        {
            Messages = messages,
            MaxTokens = 256,
            Model = settings.Version, // AnthropicModels.Claude3Haiku
            Stream = false,
            Temperature = 0m,
            SystemMessage = instruction,
            Tools = new List<Function>() { }
        };

        JsonSerializerOptions jsonSerializationOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() },
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
        };

        foreach (var fn in agent.Functions)
        {
            /*var inputschema = new InputSchema()
            {
                Type = fn.Parameters.Type,
                Properties = new Dictionary<string, Property>()
                {
                    { "location", new Property() { Type = "string", Description = "The location of the weather" } },
                    {
                        "tempType", new Property()
                        {
                            Type = "string", Enum = Enum.GetNames(typeof(TempType)),
                            Description = "The unit of temperature, celsius or fahrenheit"
                        }
                    }
                },
                Required = fn.Parameters.Required
            };*/

            string jsonString = JsonSerializer.Serialize(fn.Parameters, jsonSerializationOptions);
            parameters.Tools.Add(new Function(fn.Name, fn.Description,
                JsonNode.Parse(jsonString)));
        }

        var prompt = GetPrompt(parameters);

        return (prompt, parameters);
    }

    private string GetPrompt(MessageParameters parameters)
    {
        var prompt = $"{parameters.SystemMessage}\r\n";
        prompt += "\r\n[CONVERSATION]";

        var verbose = string.Join("\r\n", parameters.Messages
            .Select(x =>
            {
                var role = x.Role.ToString().ToLower();

                if (x.Role == RoleType.User)
                {
                    var content = string.Join("\r\n", x.Content.Select(c =>
                    {
                        if (c is TextContent text)
                            return text.Text;
                        else if (c is ToolResultContent tool)
                            return $"{tool.Content}";
                        else
                            return string.Empty;
                    }));
                    return $"{role}: {content}";
                }
                else if (x.Role == RoleType.Assistant)
                {
                    var content = string.Join("\r\n", x.Content.Select(c =>
                    {
                        if (c is TextContent text)
                            return text.Text;
                        else if (c is ToolUseContent tool)
                            return $"Call function {tool.Name}({JsonSerializer.Serialize(tool.Input)})";
                        else
                            return string.Empty;
                    }));
                    return $"{role}: {content}";
                }
                return string.Empty;
            }));

        prompt += $"\r\n{verbose}\r\n";

        if (parameters.Tools != null && parameters.Tools.Count > 0)
        {
            var functions = string.Join("\r\n", parameters.Tools.Select(x =>
            {
                return $"\r\n{x.Name}: {x.Description}\r\n{JsonSerializer.Serialize(x.Parameters)}";
            }));
            prompt += $"\r\n[FUNCTIONS]\r\n{functions}\r\n";
        }

        return prompt;
    }

    public void SetModelName(string model)
    {
        _model = model;
    }
}
