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

namespace BotSharp.Plugin.AzureOpenAI.Providers;

public class TextCompletionProvider : ITextCompletion
{
    private readonly IServiceProvider _services;
    private readonly AzureOpenAiSettings _settings;
    private readonly ILogger _logger;
    private readonly ITokenStatistics _tokenStatistics;
    private string _model;
    public string Provider => "azure-openai";

    public TextCompletionProvider(IServiceProvider services,
        AzureOpenAiSettings settings, 
        ILogger<TextCompletionProvider> logger,
        ITokenStatistics tokenStatistics)
    {
        _services = services;
        _settings = settings;
        _logger = logger;
        _tokenStatistics = tokenStatistics;
    }

    public async Task<string> GetCompletion(string text)
    {
        var (client, _) = ProviderHelper.GetClient(_model, _settings);

        var completionsOptions = new CompletionsOptions()
        {
            Prompts =
            {
                text
            },
            MaxTokens = 256,
        };
        completionsOptions.StopSequences.Add($"{AgentRole.Assistant}:");

        var state = _services.GetRequiredService<IConversationStateService>();
        var temperature = float.Parse(state.GetState("temperature", "0.5"));
        var samplingFactor = float.Parse(state.GetState("sampling_factor", "0.5"));
        completionsOptions.Temperature = temperature;
        completionsOptions.NucleusSamplingFactor = samplingFactor;

        _tokenStatistics.StartTimer();
        var response = await client.GetCompletionsAsync(
            deploymentOrModelName: _settings.DeploymentModel.TextCompletionModel,
            completionsOptions);
        _tokenStatistics.StopTimer();

        _tokenStatistics.AddToken(new TokenStatsModel
        {
            Model = _model,
            PromptCount = response.Value.Usage.PromptTokens,
            CompletionCount = response.Value.Usage.CompletionTokens,
            PromptCost = 0.0015f,
            CompletionCost = 0.002f
        });

        // OpenAI
        var completion = "";
        foreach (var t in response.Value.Choices)
        {
            completion += t.Text;
        };

        _logger.LogInformation(text);

        return completion.Trim();
    }

    public void SetModelName(string model)
    {
        _model = model;
    }
}
