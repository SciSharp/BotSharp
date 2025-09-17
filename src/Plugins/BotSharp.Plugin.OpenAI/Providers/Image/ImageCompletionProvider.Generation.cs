#pragma warning disable OPENAI001
using BotSharp.Abstraction.Hooks;
using OpenAI.Images;

namespace BotSharp.Plugin.OpenAI.Providers.Image;

public partial class ImageCompletionProvider
{
    public async Task<RoleDialogModel> GetImageGeneration(Agent agent, RoleDialogModel message)
    {
        var client = ProviderHelper.GetClient(Provider, _model, _services);
        var (prompt, imageCount, options) = PrepareGenerationOptions(message);
        var imageClient = client.GetImageClient(_model);

        var response = imageClient.GenerateImages(prompt, imageCount, options);
        var images = response.Value;

        var generatedImages = GetImageGenerations(images, options.ResponseFormat);
        var content = string.Join("\r\n", generatedImages.Where(x => !string.IsNullOrWhiteSpace(x.Description)).Select(x => x.Description));
        var responseMessage = new RoleDialogModel(AgentRole.Assistant, content)
        {
            CurrentAgentId = agent.Id,
            MessageId = message?.MessageId ?? string.Empty,
            GeneratedImages = generatedImages
        };

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

        size = settings?.Size != null ? VerifyImageParameter(size, settings.Size.Default, settings.Size.Options) : null;
        quality = settings?.Quality != null ? VerifyImageParameter(quality, settings.Quality.Default, settings.Quality.Options) : null;
        style = settings?.Style != null ? VerifyImageParameter(style, settings.Style.Default, settings.Style.Options) : null;
        responseFormat = settings?.ResponseFormat != null ? VerifyImageParameter(responseFormat, settings.ResponseFormat.Default, settings.ResponseFormat.Options) : null;
        background = settings?.Background != null ? VerifyImageParameter(background, settings.Background.Default, settings.Background.Options) : null;

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