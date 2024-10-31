using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Loggers;
using BotSharp.Abstraction.MLTasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BotSharp.Plugin.MicrosoftExtensionsAI;

/// <summary>
/// Provides an implementation of <see cref="ITextCompletion"/> for Microsoft.Extensions.AI.
/// </summary>
public sealed class MicrosoftExtensionsAITextCompletionProvider : ITextCompletion
{
    private readonly IChatClient _chatClient;
    private readonly IServiceProvider _services;
    private readonly ITokenStatistics _tokenStatistics;
    private string? _model = null;

    /// <inheritdoc/>
    public string Provider => "microsoft-extensions-ai";

    /// <summary>
    /// Creates an instance of the <see cref="MicrosoftExtensionsAITextCompletionProvider"/> class.
    /// </summary>
    public MicrosoftExtensionsAITextCompletionProvider(
        IChatClient chatClient,
        IServiceProvider services,
        ITokenStatistics tokenStatistics)
    {
        _chatClient = chatClient;
        _services = services;
        _tokenStatistics = tokenStatistics;
    }

    /// <inheritdoc/>
    public async Task<string> GetCompletion(string text, string agentId, string messageId)
    {
        var hooks = _services.GetServices<IContentGeneratingHook>().ToArray();

        // Before chat completion hook
        Agent agent = new() { Id = agentId };
        RoleDialogModel userMessage = new(AgentRole.User, text) { MessageId = messageId };
        await Task.WhenAll(hooks.Select(hook => hook.BeforeGenerating(agent, [userMessage])));

        _tokenStatistics.StartTimer();
        var completion = await _chatClient.CompleteAsync(text);
        var result = string.Concat(completion.Message.Contents.OfType<TextContent>());
        _tokenStatistics.StopTimer();

        // After chat completion hook
        await Task.WhenAll(hooks.Select(hook =>
            hook.AfterGenerated(new(AgentRole.Assistant, result), new() { Model = _model ?? "default" })));

        return result;
    }

    /// <inheritdoc/>
    public void SetModelName(string model)
    {
        if (!string.IsNullOrWhiteSpace(model))
        {
            _model = model;
        }
    }
}