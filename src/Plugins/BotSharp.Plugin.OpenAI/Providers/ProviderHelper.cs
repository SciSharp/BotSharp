using OpenAI;
using System.ClientModel;
using System.ClientModel.Primitives;

namespace BotSharp.Plugin.OpenAI.Providers;

public class ProviderHelper
{
    public static OpenAIClient GetClient(string provider, string model, string? apiKey, IServiceProvider services,
        TimeSpan? networkTimeout = null, int? maxRetries = null)
    {
        var settingsService = services.GetRequiredService<ILlmProviderService>();
        var settings = settingsService.GetSetting(provider, model);
        var options = new OpenAIClientOptions();
        if (!string.IsNullOrEmpty(settings?.Endpoint))
        {
            options.Endpoint = new Uri(settings.Endpoint);
        }
        // Left unset, the SDK defaults apply: 100s per attempt and 3 retries.
        if (networkTimeout.HasValue)
        {
            options.NetworkTimeout = networkTimeout.Value;
        }
        if (maxRetries.HasValue)
        {
            options.RetryPolicy = new ClientRetryPolicy(maxRetries.Value);
        }
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
