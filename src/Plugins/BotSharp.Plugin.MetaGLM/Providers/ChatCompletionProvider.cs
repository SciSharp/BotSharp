namespace BotSharp.Plugin.MetaGLM.Providers;

public class ChatCompletionProvider : IChatCompletion
{
    public string Provider => "metaglm";

    private readonly MetaGLMSettings _settings;
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    private readonly MetaGLMClientV4 metaGLMClient;
    private string _model;

    public ChatCompletionProvider(IServiceProvider services,
      MetaGLMSettings settings,
      MetaGLMClientV4 client,
      ILogger<ChatCompletionProvider> logger)
    {
        _services = services;
        _settings = settings;
        _logger = logger;
        metaGLMClient = client;
        _model = "glm-4";
    }

    public async Task<RoleDialogModel> GetChatCompletions(Agent agent, List<RoleDialogModel> conversations)
    {
        var contentHooks = _services.GetServices<IContentGeneratingHook>().ToList();

        // Before chat completion hook
        foreach (var hook in contentHooks)
        {
            await hook.BeforeGenerating(agent, conversations);
        }

        var dto = new TextRequestBase();
        dto.SetRequestId(Guid.NewGuid().ToString());
        dto.SetModel(_settings.ModelId);
        dto.SetTemperature(_settings.Temperature);
        dto.SetTopP(_settings.TopP);

        var prompt = PrepareOptions(agent, conversations, dto);

        var response = await metaGLMClient.Chat.Completion(dto);
        RoleDialogModel? responseMessage = null;
        if (response?.choices.FirstOrDefault()?.finish_reason == "stop")
        {
            responseMessage = new RoleDialogModel(AgentRole.Assistant, response?.choices.FirstOrDefault()?.message.content)
            {
                CurrentAgentId = agent.Id,
                MessageId = conversations.Last().MessageId
            };
        }

        if (response?.choices.FirstOrDefault()?.finish_reason == "tool_calls")
        {
            var toolcall = response.choices.FirstOrDefault()?.message.tool_calls.FirstOrDefault();
            responseMessage = new RoleDialogModel(AgentRole.Function, JsonSerializer.Serialize(toolcall.function))
            {
                CurrentAgentId = agent.Id,
                MessageId = conversations.Last().MessageId,
                FunctionName = toolcall.function.name,
                FunctionArgs = toolcall.function.arguments
            };

        }

        // After chat completion hook
        foreach (var hook in contentHooks)
        {
            await hook.AfterGenerated(message: responseMessage, new TokenStatsModel
            {
                Prompt = prompt,
                Provider = Provider,
                Model = _model,
                PromptCount = response.usage.GetValueOrDefault("prompt_tokens"),
                CompletionCount = response.usage.GetValueOrDefault("completion_tokens")
            });
        }

        return responseMessage;
    }

    private string PrepareOptions(Agent agent, List<RoleDialogModel> conversations, TextRequestBase dto)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        List<MessageItem> messages = new List<MessageItem>();
        List<FunctionTool> toolcalls = new List<FunctionTool>();

        if (!string.IsNullOrEmpty(agent.Instruction))
        {
            var instruction = agentService.RenderedInstruction(agent);
            messages.Add(new MessageItem("system", instruction));
        }

        if (!string.IsNullOrEmpty(agent.Knowledges))
        {
            messages.Add(new MessageItem("system", agent.Knowledges));
        }

        var samples = ProviderHelper.GetChatSamples(agent.Samples);
        foreach (var message in samples)
        {
            messages.Add(message.Role == AgentRole.User ?
                new MessageItem("user", message.Content) :
                new MessageItem("assistant", message.Content));
        }

        foreach (var function in agent.Functions)
        {
            var functionTool  = ConvertToFunctionTool(function);
            toolcalls.Add(functionTool);
        }

        foreach (var message in conversations)
        {
            if (message.Role == "function")
            {
                //messages.Add(ChatMessage.FromUser($"function call result: {message.content}"));
            }
            else if (message.Role == "user")
            {
                var userMessage = new MessageItem("user",message.Content);

                messages.Add(userMessage);
            }
            else if (message.Role == "assistant")
            {
                messages.Add(new MessageItem("assistant", message.Content));
            }
        }

        if (toolcalls.Count > 0)
        {
            dto.SetTools(toolcalls.ToArray());
            dto.SetToolChoice("auto");
        }
        dto.SetMessages(messages.ToArray());
         
        var prompt = GetPrompt(messages, toolcalls);

        //var state = _services.GetRequiredService<IConversationStateService>();
        //var temperature = float.Parse(state.GetState("temperature", "0.0"));
        //var samplingFactor = float.Parse(state.GetState("sampling_factor", "0.0"));
        //dto.SetTemperature(temperature);  

        return prompt;
    }

    private FunctionTool ConvertToFunctionTool(FunctionDef def)
    {
        var functionTool = new FunctionTool()
        {
            type = "function"
        
        }
            .SetName(def.Name)
            .SetDescription(def.Description);

        var funcParameter = new FunctionParameters() { type = def.Parameters.Type };
        funcParameter.SetRequiredParameter(def.Parameters.Required.ToArray());

        var parameters = def.Parameters;
        var funcParamsProperties = parameters.Properties;
          
        if (funcParamsProperties != null)
        {
            var props = funcParamsProperties.RootElement.EnumerateObject();
            while (props.MoveNext())
            {
                var prop = props.Current;
                var name = prop.Name;
                string typestr = prop.Value.GetProperty("type").GetRawText();
                if (!string.IsNullOrEmpty(typestr))
                {
                    ParameterType parameterType;
                    if (Enum.TryParse(typestr, out parameterType))
                    {
                        funcParameter.AddParameter(name, parameterType, prop.Value.GetProperty("description").GetRawText());
                    }
                    else
                    {
                         
                    }

                }
            }
        }
        functionTool.SetParameters(funcParameter);
        return functionTool;
    }

    private string GetPrompt(List<MessageItem> messages, List<FunctionTool> functions)
    {
        var prompt = string.Empty;

        if (messages.Count > 0)
        {
            // System instruction
            var verbose = string.Join("\r\n", messages
                .Where(x => x.role == AgentRole.System)
                .Select(x =>
                {
                    return $"{x.role}: {x.content}";
                }));
            prompt += $"{verbose}\r\n";

            verbose = string.Join("\r\n", messages
                .Where(x => x.role != AgentRole.System).Select(x =>
                {
                    return
                        $"{x.role}: {x.content}";

                }));
            prompt += $"\r\n{verbose}\r\n";
        }
        if(functions.Count > 0)
        {

        }
        return prompt;
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
