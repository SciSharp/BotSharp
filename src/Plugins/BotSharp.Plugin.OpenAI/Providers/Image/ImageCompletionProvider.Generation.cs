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

        var state = _services.GetRequiredService<IConversationStateService>();
        var size = GetImageSize(state.GetState("image_size"));
        var quality = GetImageQuality(state.GetState("image_quality"));
        var style = GetImageStyle(state.GetState("image_style"));
        var format = GetImageFormat(state.GetState("image_format"));
        var count = GetImageCount(state.GetState("image_count", "1"));

        var options = new ImageGenerationOptions
        {
            Size = size,
            Quality = quality,
            Style = style,
            ResponseFormat = format
        };
        return (prompt, count, options);
    }
}