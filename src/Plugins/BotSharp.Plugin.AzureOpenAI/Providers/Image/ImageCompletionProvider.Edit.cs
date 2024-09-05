using OpenAI.Images;

namespace BotSharp.Plugin.AzureOpenAI.Providers.Image;

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

        var state = _services.GetRequiredService<IConversationStateService>();
        var size = GetImageSize(state.GetState("image_size"));
        var format = GetImageFormat(state.GetState("image_response_format"));
        var count = GetImageCount(state.GetState("image_count", "1"));

        var options = new ImageEditOptions
        {
            Size = size,
            ResponseFormat = format
        };
        return (prompt, count, options);
    }
}
