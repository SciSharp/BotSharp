using Azure.AI.OpenAI;
using BotSharp.Abstraction.MLTasks;
using System;
using System.Threading.Tasks;
using BotSharp.Plugin.AzureOpenAI.Settings;
using Microsoft.Extensions.Logging;
using BotSharp.Abstraction.Conversations;
using Microsoft.Extensions.DependencyInjection;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Agents.Enums;
using System.Linq;
using System.Collections.Generic;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Loggers;

namespace BotSharp.Plugin.AzureOpenAI.Providers;

public class TextCompletionProvider : ITextCompletion
{
    private readonly IServiceProvider _services;
    private readonly AzureOpenAiSettings _settings;
    private string _model;
    public string Provider => "azure-openai";

    public TextCompletionProvider(IServiceProvider services,
        AzureOpenAiSettings settings)
    {
        _services = services;
        _settings = settings;
    }

    public async Task<string> GetCompletion(string text, string agentId, string messageId)
    {
        var contentHooks = _services.GetServices<IContentGeneratingHook>().ToList();
        var logHook = _services.GetService<IVerboseLogHook>();

        // Before chat completion hook
        var agent = new Agent()
        {
            Id = agentId,
        };
        var message = new RoleDialogModel(AgentRole.User, text)
        {
            CurrentAgentId = agentId,
            MessageId = messageId
        };

        Task.WaitAll(contentHooks.Select(hook =>
            hook.BeforeGenerating(agent,
                new List<RoleDialogModel>
                {
                    message
                })).ToArray());

        var client = ProviderHelper.GetClient(_model, _settings);

        var completionsOptions = new CompletionsOptions()
        {
            Prompts =
            {
                text
            },
            MaxTokens = 256,
        };
        completionsOptions.StopSequences.Add($"{AgentRole.Assistant}:");
        logHook?.GenerateLog(text);

        var state = _services.GetRequiredService<IConversationStateService>();
        var temperature = float.Parse(state.GetState("temperature", "0.5"));
        var samplingFactor = float.Parse(state.GetState("sampling_factor", "0.5"));
        completionsOptions.Temperature = temperature;
        completionsOptions.NucleusSamplingFactor = samplingFactor;
        completionsOptions.DeploymentName = _model;
        var response = await client.GetCompletionsAsync(completionsOptions);

        // OpenAI
        var completion = "";
        foreach (var t in response.Value.Choices)
        {
            completion += t.Text;
        };

        logHook?.GenerateLog(completion);

        // After chat completion hook
        var responseMessage = new RoleDialogModel(AgentRole.Assistant, completion)
        {
            CurrentAgentId = agentId,
            MessageId = messageId
        };
        Task.WaitAll(contentHooks.Select(hook =>
            hook.AfterGenerated(responseMessage, new TokenStatsModel
            {
                Prompt = text,
                Model = _model,
                PromptCount = response.Value.Usage.PromptTokens,
                CompletionCount = response.Value.Usage.CompletionTokens
            })).ToArray());

        return completion.Trim();
    }

    public void SetModelName(string model)
    {
        _model = model;
    }
}
