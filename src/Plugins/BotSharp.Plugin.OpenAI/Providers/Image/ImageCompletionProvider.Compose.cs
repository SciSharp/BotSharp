#pragma warning disable OPENAI001
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
        var contentHooks = _services.GetHooks<IContentGeneratingHook>(agent.Id);

        // Before generating hook
        foreach (var hook in contentHooks)
        {
            await hook.BeforeGenerating(agent, [message]);
        }

        var client = ProviderHelper.GetClient(Provider, _model, _services);
        var (prompt, imageCount, options) = PrepareEditOptions(message);
        var imageClient = client.GetImageClient(_model);

        // Use the new extension method to support multiple images
        var response = imageClient.GenerateImageEdits(_model, images, imageFileNames, prompt, imageCount, options);
        var rawContent = response.GetRawResponse().Content.ToString();
        var responseModel = JsonSerializer.Deserialize<ImageGenerationResponse>(rawContent, BotSharpOptions.defaultJsonOptions);
        var generatedImageCollection = response.Value;

        var generatedImages = GetImageGenerations(generatedImageCollection, options.ResponseFormat);
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
                TextInputTokens = generatedImageCollection?.Usage?.InputTokenDetails?.TextTokenCount ?? 0,
                ImageInputTokens = generatedImageCollection?.Usage?.InputTokenDetails?.ImageTokenCount ?? 0,
                ImageOutputTokens = generatedImageCollection?.Usage?.OutputTokenCount ?? 0,
                ImageGenerationCount = imageCount,
                ImageGenerationUnitCost = unitCost
            });
        }

        return await Task.FromResult(responseMessage);
    }
}

