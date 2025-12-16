#pragma warning disable OPENAI001
using OpenAI.Images;

namespace BotSharp.Plugin.OpenAI.Providers.Image;

public partial class ImageCompletionProvider
{
    public async Task<RoleDialogModel> GetImageGeneration(Agent agent, RoleDialogModel message)
    {
        var contentHooks = _services.GetHooks<IContentGeneratingHook>(agent.Id);

        // Before generating hook
        foreach (var hook in contentHooks)
        {
            await hook.BeforeGenerating(agent, [message]);
        }

        var client = ProviderHelper.GetClient(Provider, _model, _services);
        var (prompt, imageCount, options) = PrepareGenerationOptions(message);
        var imageClient = client.GetImageClient(_model);

        var response = imageClient.GenerateImages(prompt, imageCount, options);
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
        var unitCost = GetImageGenerationUnitCost(_model, responseModel?.Quality, responseModel?.Size);
        foreach (var hook in contentHooks)
        {
            await hook.AfterGenerated(responseMessage, new TokenStatsModel
            {
                Prompt = prompt,
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

    private (string, int, ImageGenerationOptions) PrepareGenerationOptions(RoleDialogModel message)
    {
        var prompt = message?.Payload ?? message?.Content ?? string.Empty;

        var settingsService = _services.GetRequiredService<ILlmProviderService>();
        var state = _services.GetRequiredService<IConversationStateService>();
        
        var size = state.GetState("image_size");
        var quality = state.GetState("image_quality");
        var style = state.GetState("image_style");
        var responseFormat = state.GetState("image_response_format");
        var background = state.GetState("image_background");

        var settings = settingsService.GetSetting(Provider, _model)?.Image?.Generation;

        size = settings?.Size != null ? LlmUtility.VerifyModelParameter(size, settings.Size.Default, settings.Size.Options) : null;
        quality = settings?.Quality != null ? LlmUtility.VerifyModelParameter(quality, settings.Quality.Default, settings.Quality.Options) : null;
        style = settings?.Style != null ? LlmUtility.VerifyModelParameter(style, settings.Style.Default, settings.Style.Options) : null;
        responseFormat = settings?.ResponseFormat != null ? LlmUtility.VerifyModelParameter(responseFormat, settings.ResponseFormat.Default, settings.ResponseFormat.Options) : null;
        background = settings?.Background != null ? LlmUtility.VerifyModelParameter(background, settings.Background.Default, settings.Background.Options) : null;

        var options = new ImageGenerationOptions();
        if (!string.IsNullOrEmpty(size))
        {
            options.Size = GetImageSize(size);
        }
        if (!string.IsNullOrEmpty(quality))
        {
            options.Quality = GetImageQuality(quality);
        }
        if (!string.IsNullOrEmpty(style))
        {
            options.Style = GetImageStyle(style);
        }
        if (!string.IsNullOrEmpty(responseFormat))
        {
            options.ResponseFormat = GetImageResponseFormat(responseFormat);
        }
        if (!string.IsNullOrEmpty(background))
        {
            options.Background = GetImageBackground(background);
        }

        var count = GetImageCount(state.GetState("image_count"));
        return (prompt, count, options);
    }
}