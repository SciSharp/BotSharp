using Azure.AI.OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace BotSharp.Plugin.AzureOpenAI.Providers;

public class ProviderHelper
{
    public const string MaxTokensMessage = "AI response exceeded max output length";

    /// <summary>
    /// Content of the final streamed message. A reply cut off by the output limit gets the same notice as the
    /// non-streaming path instead of silently passing the truncated text off as a complete answer.
    /// </summary>
    public static string ResolveStreamedContent(ChatFinishReason reason, string collectedText)
    {
        return reason == ChatFinishReason.Length ? MaxTokensMessage : collectedText;
    }

    public static AzureOpenAIClient GetClient(string provider, string model, IServiceProvider services)
    {
        var settingsService = services.GetRequiredService<ILlmProviderService>();
        var settings = settingsService.GetSetting(provider, model);
        var client = new AzureOpenAIClient(new Uri(settings.Endpoint), new ApiKeyCredential(settings.ApiKey));
        return client;
    }

    public static List<RoleDialogModel> GetChatSamples(List<string> lines)
    {
        var samples = new List<RoleDialogModel>();

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            if (string.IsNullOrEmpty(line.Trim()))
            {
                continue;
            }
            var role = line.Substring(0, line.IndexOf(' ') - 1).Trim();
            var content = line.Substring(line.IndexOf(' ') + 1).Trim();

            // comments
            if (role == "##")
            {
                continue;
            }

            samples.Add(new RoleDialogModel(role, content));
        }

        return samples;
    }
}
