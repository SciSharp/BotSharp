#pragma warning disable OPENAI001
using OpenAI.Images;

namespace BotSharp.Plugin.OpenAI.Providers.Image;

public partial class ImageCompletionProvider
{
    public async Task<RoleDialogModel> GetImageVariation(Agent agent, RoleDialogModel message, Stream image, string imageFileName)
    {
        var contentHooks = _services.GetHooks<IContentGeneratingHook>(agent.Id);

        // Before generating hook
        foreach (var hook in contentHooks)
        {
            await hook.BeforeGenerating(agent, [message]);
        }

        var settingsService = _services.GetRequiredService<ILlmProviderService>();
        var settings = settingsService.GetSetting(Provider, _model);

        var client = ProviderHelper.GetClient(Provider, _model, apiKey: null, _services);
        var (imageCount, options) = PrepareVariationOptions(settings?.Image?.Variation);
        var imageClient = client.GetImageClient(_model);

        var response = imageClient.GenerateImageVariations(image, imageFileName, imageCount, options);
        var rawContent = response.GetRawResponse().Content.ToString();
        var responseModel = JsonSerializer.Deserialize<ImageGenerationResponse>(rawContent, BotSharpOptions.defaultJsonOptions);
        var images = response.Value;

        var generatedImages = GetImageGenerations(images, options.ResponseFormat);
        var content = string.Join("\r\n", generatedImages.Where(x => !string.IsNullOrWhiteSpace(x.Description)).Select(x => x.Description));
        var responseMessage = new RoleDialogModel(AgentRole.Assistant, content)
        {
            CurrentAgentId = agent.Id,
            MessageId = message?.MessageId ?? string.Empty,
            GeneratedImages = generatedImages
        };

        // After generating hook
        var unitCost = GetImageGenerationUnitCost(settings?.Cost?.ImageCosts, responseModel?.Quality, responseModel?.Size);
        foreach (var hook in contentHooks)
        {
            await hook.AfterGenerated(responseMessage, new TokenStatsModel
            {
                Prompt = string.Empty,
                Provider = Provider,
                Model = _model,
                TextInputTokens = images?.Usage?.InputTokenDetails?.TextTokenCount ?? 0,
                ImageInputTokens = images?.Usage?.InputTokenDetails?.ImageTokenCount ?? 0,
                ImageOutputTokens = images?.Usage?.OutputTokenCount ?? 0,
                ImageGenerationCount = imageCount,
                ImageGenerationUnitCost = unitCost
            });
        }

        return await Task.FromResult(responseMessage);
    }

    private (int, ImageVariationOptions) PrepareVariationOptions(ImageVariationSetting? settings)
    {
        var state = _services.GetRequiredService<IConversationStateService>();

        var size = state.GetState("image_size");
        var responseFormat = state.GetState("image_response_format");

        size = LlmUtility.GetModelParameter(settings?.Parameters, "Size", size);
        responseFormat = LlmUtility.GetModelParameter(settings?.Parameters, "ResponseFormat", responseFormat);

        var options = new ImageVariationOptions();
        if (!string.IsNullOrEmpty(size))
        {
            options.Size = GetImageSize(size);
        }
        if (!string.IsNullOrEmpty(responseFormat))
        {
            options.ResponseFormat = GetImageResponseFormat(responseFormat);
        }

        var count = GetImageCount(state.GetState("image_count", "1"));
        return (count, options);
    }
}
