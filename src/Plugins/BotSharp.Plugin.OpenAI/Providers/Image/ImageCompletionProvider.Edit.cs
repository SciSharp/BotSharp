#pragma warning disable OPENAI001
using OpenAI.Images;

namespace BotSharp.Plugin.OpenAI.Providers.Image;

public partial class ImageCompletionProvider
{
    public async Task<RoleDialogModel> GetImageEdits(Agent agent, RoleDialogModel message, Stream image, string imageFileName)
    {
        var client = ProviderHelper.GetClient(Provider, _model, _services);
        var (prompt, imageCount, options) = PrepareEditOptions(message);
        var imageClient = client.GetImageClient(_model);

        var response = imageClient.GenerateImageEdits(image, imageFileName, prompt, imageCount, options);
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

    public async Task<RoleDialogModel> GetImageEdits(Agent agent, RoleDialogModel message,
        Stream image, string imageFileName, Stream mask, string maskFileName)
    {
        var client = ProviderHelper.GetClient(Provider, _model, _services);
        var (prompt, imageCount, options) = PrepareEditOptions(message);
        var imageClient = client.GetImageClient(_model);

        var response = imageClient.GenerateImageEdits(image, imageFileName, prompt, mask, maskFileName, imageCount, options);
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

    private (string, int, ImageEditOptions) PrepareEditOptions(RoleDialogModel message)
    {
        var prompt = message?.Payload ?? message?.Content ?? string.Empty;

        var settingsService = _services.GetRequiredService<ILlmProviderService>();
        var state = _services.GetRequiredService<IConversationStateService>();

        var size = state.GetState("image_size");
        var responseFormat = state.GetState("image_response_format");
        var background = state.GetState("image_background");

        var settings = settingsService.GetSetting(Provider, _model)?.Image?.Edit;

        size = settings?.Size != null ? LlmUtility.VerifyModelParameter(size, settings.Size.Default, settings.Size.Options) : null;
        responseFormat = settings?.ResponseFormat != null ? LlmUtility.VerifyModelParameter(responseFormat, settings.ResponseFormat.Default, settings.ResponseFormat.Options) : null;
        background = settings?.Background != null ? LlmUtility.VerifyModelParameter(background, settings.Background.Default, settings.Background.Options) : null;

        var options = new ImageEditOptions();
        if (!string.IsNullOrEmpty(size))
        {
            options.Size = GetImageSize(size);
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
