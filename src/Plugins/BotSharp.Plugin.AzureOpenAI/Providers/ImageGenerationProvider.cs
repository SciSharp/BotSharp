using Azure.AI.OpenAI;
using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Loggers;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Plugin.AzureOpenAI.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BotSharp.Plugin.AzureOpenAI.Providers;

public class ImageGenerationProvider : IImageGeneration
{
    protected readonly AzureOpenAiSettings _settings;
    protected readonly IServiceProvider _services;
    protected readonly ILogger _logger;

    protected string _model;

    public virtual string Provider => "azure-openai";

    public ImageGenerationProvider(
        AzureOpenAiSettings settings,
        ILogger<ImageGenerationProvider> logger,
        IServiceProvider services)
    {
        _settings = settings;
        _services = services;
        _logger = logger;
    }


    public async Task<RoleDialogModel> GetImageGeneration(Agent agent, List<RoleDialogModel> conversations)
    {
        var contentHooks = _services.GetServices<IContentGeneratingHook>().ToList();

        // Before
        foreach (var hook in contentHooks)
        {
            await hook.BeforeGenerating(agent, conversations);
        }

        var client = ProviderHelper.GetClient(Provider, _model, _services);
        var options = PrepareOptions(conversations);
        var response = await client.GetImageGenerationsAsync(options);
        var image = response.Value.Data.First();

        var content = string.Empty;
        if (!string.IsNullOrEmpty(image.RevisedPrompt))
        {
            content = image.RevisedPrompt;
        }

        var responseMessage = new RoleDialogModel(AgentRole.Assistant, content)
        {
            CurrentAgentId = agent.Id,
            MessageId = conversations.LastOrDefault()?.MessageId ?? string.Empty,
            Data = image.Url.AbsoluteUri ?? image.Base64Data
        };

        // After
        foreach (var hook in contentHooks)
        {
            await hook.AfterGenerated(responseMessage, new TokenStatsModel
            {
                Prompt = options.Prompt,
                Provider = Provider,
                Model = _model,
                PromptCount = options.Prompt.Split(' ', StringSplitOptions.RemoveEmptyEntries).Count(),
                CompletionCount = content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Count()
            });
        }

        return responseMessage;
    }

    private ImageGenerationOptions PrepareOptions(List<RoleDialogModel> conversations)
    {
        var state = _services.GetRequiredService<IConversationStateService>();

        var sizeValue = !string.IsNullOrEmpty(state.GetState("image_size")) ? state.GetState("image_size") : "1024x1024";
        var qualityValue = !string.IsNullOrEmpty(state.GetState("image_quality")) ? state.GetState("image_quality") : "standard";

        var options = new ImageGenerationOptions
        {
            DeploymentName = _model,
            Prompt = conversations.LastOrDefault()?.Payload ?? conversations.LastOrDefault()?.Content ?? string.Empty,
            Size = new ImageSize(sizeValue),
            Quality = new ImageGenerationQuality(qualityValue)
        };
        return options;
    }

    public void SetModelName(string model)
    {
        _model = model;
    }
}
