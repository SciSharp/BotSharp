namespace BotSharp.Plugin.ImageHandler.Helpers;

internal static class AiResponseHelper
{
    internal static string GetDefaultResponse(IEnumerable<string> files)
    {
        if (files.IsNullOrEmpty())
        {
            return $"No image is generated.";
        }

        if (files.Count() > 1)
        {
            return $"Here are the images you asked for: {string.Join(", ", files)}";
        }

        return $"Here is the image you asked for: {string.Join(", ", files)}";
    }

    internal static async Task<string> GetImageGenerationResponse(IServiceProvider services, Agent agent, string description)
    {
        var text = $"Please generate a user-friendly response from the following description to " +
                   $"inform user that you have completed the required image: {description}";

        var provider = agent?.LlmConfig?.Provider ?? "openai";
        var model = agent?.LlmConfig?.Model ?? "gpt-4o-mini";
        var completion = CompletionProvider.GetChatCompletion(services, provider: provider, model: model);
        var response = await completion.GetChatCompletions(agent, [new RoleDialogModel(AgentRole.User, text)]);
        return response.Content;
    }
}
