using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Files;
using BotSharp.Abstraction.Files.Utilities;
using BotSharp.Abstraction.Loggers;
using BotSharp.Abstraction.MLTasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace BotSharp.Plugin.MicrosoftExtensionsAI;

/// <summary>
/// Provides an implementation of <see cref="IChatCompletion"/> for Microsoft.Extensions.AI.
/// </summary>
public sealed class MicrosoftExtensionsAIChatCompletionProvider : IChatCompletion
{
    private readonly IChatClient _client;
    private readonly ILogger<MicrosoftExtensionsAIChatCompletionProvider> _logger;
    private readonly IServiceProvider _services;
    private string? _model;

    /// <summary>
    /// Creates an instance of the <see cref="MicrosoftExtensionsAIChatCompletionProvider"/> class.
    /// </summary>
    public MicrosoftExtensionsAIChatCompletionProvider(
        IChatClient client,
        ILogger<MicrosoftExtensionsAIChatCompletionProvider> logger,
        IServiceProvider services)
    {
        _client = client;
        _model = _client.Metadata.ModelId;
        _logger = logger;
        _services = services;
    }
    
    /// <inheritdoc/>
    public string Provider => "microsoft.extensions.ai";

    /// <inheritdoc/>
    public void SetModelName(string model) => _model = model;

    /// <inheritdoc/>
    public async Task<RoleDialogModel> GetChatCompletions(Agent agent, List<RoleDialogModel> conversations)
    {
        // Before chat completion hook
        var hooks = _services.GetServices<IContentGeneratingHook>().ToArray();
        await Task.WhenAll(hooks.Select(hook => hook.BeforeGenerating(agent, conversations)));

        // Configure options
        var state = _services.GetRequiredService<IConversationStateService>();
        var options = new ChatOptions()
        {
            Temperature = float.Parse(state.GetState("temperature", "0.0")),
            MaxOutputTokens = int.Parse(state.GetState("max_tokens", "1024"))
        };

        if (_services.GetService<IAgentService>() is { } agentService)
        {
            foreach (var function in agent.Functions)
            {
                if (agentService.RenderFunction(agent, function))
                {
                    var property = agentService.RenderFunctionProperty(agent, function);
                    (options.Tools ??= []).Add(new NopAIFunction(new(function.Name)
                    {
                        Description = function.Description,
                        Parameters = property?.Properties.RootElement.Deserialize<Dictionary<string, object?>>()?.Select(p => new AIFunctionParameterMetadata(p.Key)
                        {
                            Schema = p.Value,
                        }).ToList() ?? [],
                    }));
                }
            }
        }

        // Configure messages
        List<ChatMessage> messages = [];

        if (_services.GetRequiredService<IAgentService>().RenderedInstruction(agent) is string instruction &&
            instruction.Length > 0)
        {
            messages.Add(new(ChatRole.System, instruction));
        }

        if (!string.IsNullOrEmpty(agent.Knowledges))
        {
            messages.Add(new(ChatRole.System, agent.Knowledges));
        }

        foreach (var sample in ProviderHelper.GetChatSamples(agent.Samples))
        {
            messages.Add(new(sample.Role == AgentRole.Assistant ? ChatRole.Assistant : ChatRole.User, sample.Content));
        }

        var fileStorage = _services.GetService<IFileStorageService>();
        bool allowMultiModal = fileStorage is not null && _services.GetService<ILlmProviderService>()?.GetSetting(Provider, _model ?? "default")?.MultiModal is true;
        foreach (var x in conversations)
        {
            if (x.Role == AgentRole.Function && x.FunctionName is not null)
            {
                messages.Add(new(ChatRole.Assistant,
                [
                    new FunctionCallContent(x.FunctionName, x.FunctionName, JsonSerializer.Deserialize<Dictionary<string, object?>>(x.FunctionArgs ?? "{}")),
                    new FunctionResultContent(x.FunctionName, x.FunctionName, x.Content)
                ]));
            }
            else if (x.Role == AgentRole.System || x.Role == AgentRole.Assistant)
            {
                messages.Add(new(x.Role == AgentRole.System ? ChatRole.System : ChatRole.Assistant, x.Content));
            }
            else if (x.Role == AgentRole.User)
            {
                List<AIContent> contents = [new TextContent(!string.IsNullOrWhiteSpace(x.Payload) ? x.Payload : x.Content)];
                if (allowMultiModal)
                {
                    foreach (var file in x.Files)
                    {
                        if (!string.IsNullOrEmpty(file.FileData))
                        {
                            contents.Add(new ImageContent(file.FileData));
                        }
                        else if (!string.IsNullOrEmpty(file.FileStorageUrl))
                        {
                            var contentType = FileUtility.GetFileContentType(file.FileStorageUrl);
                            var bytes = fileStorage!.GetFileBytes(file.FileStorageUrl);
                            contents.Add(new ImageContent(bytes, contentType));
                        }
                        else if (!string.IsNullOrEmpty(file.FileUrl))
                        {
                            contents.Add(new ImageContent(file.FileUrl));
                        }
                    }
                }

                messages.Add(new(ChatRole.User, contents) { AuthorName = x.FunctionName });
            }
        }

        var completion = await _client.CompleteAsync(messages);

        RoleDialogModel result = new(AgentRole.Assistant, string.Concat(completion.Message.Contents.OfType<TextContent>()))
        {
            CurrentAgentId = agent.Id
        };

        if (completion.Message.Contents.OfType<FunctionCallContent>().FirstOrDefault() is { } fcc)
        {
            result.Role = AgentRole.Function;
            result.MessageId = conversations.LastOrDefault()?.MessageId ?? string.Empty;
            result.FunctionName = fcc.Name;
            result.FunctionArgs = fcc.Arguments is not null ? JsonSerializer.Serialize(fcc.Arguments) : null;
        }

        // After chat completion hook
        await Task.WhenAll(hooks.Select(hook => hook.AfterGenerated(result, new() { Model = _model ?? "default" })));

        return result;
    }

    /// <inheritdoc/>
    public Task<bool> GetChatCompletionsAsync(Agent agent, List<RoleDialogModel> conversations, Func<RoleDialogModel, Task> onMessageReceived, Func<RoleDialogModel, Task> onFunctionExecuting) =>
        throw new NotImplementedException();

    /// <inheritdoc/>
    public Task<bool> GetChatCompletionsStreamingAsync(Agent agent, List<RoleDialogModel> conversations, Func<RoleDialogModel, Task> onMessageReceived) =>
        throw new NotImplementedException();

    private sealed class NopAIFunction(AIFunctionMetadata metadata) : AIFunction
    {
        public override AIFunctionMetadata Metadata { get; } = metadata;

        protected override Task<object?> InvokeCoreAsync(IEnumerable<KeyValuePair<string, object?>> arguments, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}