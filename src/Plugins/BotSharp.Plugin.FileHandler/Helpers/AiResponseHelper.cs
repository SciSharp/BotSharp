using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.Plugin.FileHandler.Helpers;

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
        var text = $"Please generate a user-friendly and short response from the following description to " +
                   $"inform user that the required image is generated : {description}";

        var agentSettings = services.GetRequiredService<AgentSettings>();
        var provider = agent?.LlmConfig?.Provider ?? agentSettings.LlmConfig.Provider;
        var model = agent?.LlmConfig?.Model ?? agentSettings.LlmConfig.Model;
        var completion = CompletionProvider.GetChatCompletion(services, provider: provider, model: model);
        var response = await completion.GetChatCompletions(agent, [new RoleDialogModel(AgentRole.User, text)]);
        return response.Content;
    }
}
