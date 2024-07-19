using OpenAI.Images;

namespace BotSharp.Plugin.AzureOpenAI.Providers.Image;

public partial class ImageCompletionProvider
{
    public async Task<RoleDialogModel> GetImageVariation(Agent agent, RoleDialogModel message, Stream image, string imageFileName)
    {
        var client = ProviderHelper.GetClient(Provider, _model, _services);
        var (imageCount, options) = PrepareOptions();
        var imageClient = client.GetImageClient(_model);

        var response = imageClient.GenerateImageVariations(image, imageFileName, imageCount, options);
        var values = response.Value;

        var generatedImages = new List<ImageGeneration>();
        foreach (var value in values)
        {
            if (value == null) continue;

            var generatedImage = new ImageGeneration { Description = value?.RevisedPrompt ?? string.Empty };
            if (options.ResponseFormat == GeneratedImageFormat.Uri)
            {
                generatedImage.ImageUrl = value?.ImageUri?.AbsoluteUri ?? string.Empty;
            }
            else if (options.ResponseFormat == GeneratedImageFormat.Bytes)
            {
                var base64Str = string.Empty;
                var bytes = value?.ImageBytes?.ToArray();
                if (!bytes.IsNullOrEmpty())
                {
                    base64Str = Convert.ToBase64String(bytes);
                }
                generatedImage.ImageData = base64Str;
            }

            generatedImages.Add(generatedImage);
        }

        var content = string.Join("\r\n", generatedImages.Where(x => !string.IsNullOrWhiteSpace(x.Description)).Select(x => x.Description));
        var responseMessage = new RoleDialogModel(AgentRole.Assistant, content)
        {
            CurrentAgentId = agent.Id,
            MessageId = message?.MessageId ?? string.Empty,
            GeneratedImages = generatedImages
        };

        return await Task.FromResult(responseMessage);
    }

    private (int, ImageVariationOptions) PrepareOptions()
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        var size = state.GetState("image_size");
        var format = state.GetState("image_format");
        var count = GetImageCount(state.GetState("image_count", "1"));

        var options = new ImageVariationOptions
        {
            Size = GetImageSize(size),
            ResponseFormat = GetImageFormat(format)
        };
        return (count, options);
    }
}
