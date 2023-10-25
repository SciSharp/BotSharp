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
using BotSharp.Abstraction.Conversations.Settings;

namespace BotSharp.Plugin.AzureOpenAI.Providers;

public class TextCompletionProvider : ITextCompletion
{
    private readonly IServiceProvider _services;
    private readonly AzureOpenAiSettings _settings;
    private readonly ILogger _logger;
    private string _model;
    public string Provider => "azure-openai";

    public TextCompletionProvider(IServiceProvider services,
        AzureOpenAiSettings settings, 
        ILogger<TextCompletionProvider> logger)
    {
        _services = services;
        _settings = settings;
        _logger = logger;
    }

    public async Task<string> GetCompletion(string text)
    {
        var hooks = _services.GetServices<IContentGeneratingHook>().ToList();

        // Before chat completion hook
        Task.WaitAll(hooks.Select(hook =>
            hook.BeforeGenerating(new Agent(), 
                new List<RoleDialogModel> 
                { 
                    new RoleDialogModel(AgentRole.User, text) 
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

        var setting = _services.GetRequiredService<ConversationSetting>();
        if (setting.ShowVerboseLog)
        {
            _logger.LogInformation(text);
        }

        var state = _services.GetRequiredService<IConversationStateService>();
        var temperature = float.Parse(state.GetState("temperature", "0.5"));
        var samplingFactor = float.Parse(state.GetState("sampling_factor", "0.5"));
        completionsOptions.Temperature = temperature;
        completionsOptions.NucleusSamplingFactor = samplingFactor;

        var response = await client.GetCompletionsAsync(
            deploymentOrModelName: _model,
            completionsOptions);

        // OpenAI
        var completion = "";
        foreach (var t in response.Value.Choices)
        {
            completion += t.Text;
        };

        if (setting.ShowVerboseLog)
        {
            _logger.LogInformation(completion);
        }

        // After chat completion hook
        Task.WaitAll(hooks.Select(hook =>
            hook.AfterGenerated(new RoleDialogModel(AgentRole.Assistant, completion), new TokenStatsModel
            {
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
