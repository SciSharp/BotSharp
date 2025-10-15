#pragma warning disable OPENAI001
using OpenAI.Images;

namespace BotSharp.Plugin.OpenAI.Providers.Image;

public partial class ImageCompletionProvider
{
    /// <summary>
    /// Composes multiple images into a single image using OpenAI's image edit API
    /// </summary>
    /// <param name="agent">The agent making the request</param>
    /// <param name="message">The message containing the composition prompt</param>
    /// <param name="images">Array of image streams to compose</param>
    /// <param name="imageFileNames">Array of corresponding file names</param>
    /// <returns>RoleDialogModel containing the composed image(s)</returns>
    public async Task<RoleDialogModel> GetImageComposition(Agent agent, RoleDialogModel message, Stream[] images, string[] imageFileNames)
    {
        var client = ProviderHelper.GetClient(Provider, _model, _services);
        var (prompt, imageCount, options) = PrepareEditOptions(message);
        var imageClient = client.GetImageClient(_model);

        // Use the new extension method to support multiple images
        options.ResponseFormat = "b64_json";
        options.Quality = "medium";
        options.Background = "auto";
        options.Size = GeneratedImageSize.Auto;
        var response = imageClient.GenerateImageEdits(images, imageFileNames, prompt, imageCount, options);
        var generatedImageCollection = response.Value;

        var generatedImages = GetImageGenerations(generatedImageCollection, options.ResponseFormat);
        var content = string.Join("\r\n", generatedImages.Where(x => !string.IsNullOrWhiteSpace(x.Description)).Select(x => x.Description));
        var responseMessage = new RoleDialogModel(AgentRole.Assistant, content)
        {
            CurrentAgentId = agent.Id,
            MessageId = message?.MessageId ?? string.Empty,
            GeneratedImages = generatedImages
        };

        return await Task.FromResult(responseMessage);
    }
}

