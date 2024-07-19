using OpenAI.Images;

namespace BotSharp.Plugin.OpenAI.Providers.Image;

public partial class ImageCompletionProvider
{
    public async Task<RoleDialogModel> GetImageVariation(Agent agent, RoleDialogModel message, Stream image, string imageFileName)
    {
        var client = ProviderHelper.GetClient(Provider, _model, _services);
        var (imageCount, options) = PrepareVariationOptions();
        var imageClient = client.GetImageClient(_model);

        var response = imageClient.GenerateImageVariations(image, imageFileName, imageCount, options);
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

    private (int, ImageVariationOptions) PrepareVariationOptions()
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        var size = GetImageSize(state.GetState("image_size"));
        var format = GetImageFormat(state.GetState("image_format"));
        var count = GetImageCount(state.GetState("image_count", "1"));

        var options = new ImageVariationOptions
        {
            Size = size,
            ResponseFormat = format
        };
        return (count, options);
    }
}
