using OpenAI;
using System.ClientModel;

namespace BotSharp.Plugin.OpenAI.Providers;

public class ProviderHelper
{
    public static OpenAIClient GetClient(string provider, string model, string? apiKey, IServiceProvider services)
    {
        var settingsService = services.GetRequiredService<ILlmProviderService>();
        var settings = settingsService.GetSetting(provider, model);
        if (settings == null && string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException($"No LLM model settings found for '{provider}.{model}'. Register the model under LlmProviders (appsettings/user secrets) or pass an api key.");
        }
        var options = !string.IsNullOrEmpty(settings?.Endpoint) ?
                        new OpenAIClientOptions { Endpoint = new Uri(settings.Endpoint) } : null;
        return new OpenAIClient(new ApiKeyCredential(apiKey ?? settings!.ApiKey), options);
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
