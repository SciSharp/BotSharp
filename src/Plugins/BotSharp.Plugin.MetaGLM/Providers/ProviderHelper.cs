namespace BotSharp.Plugin.MetaGLM.Providers;

internal class ProviderHelper
{
    public static string GetClient(string model, IServiceProvider services)
    {
        var settingsService = services.GetRequiredService<ILlmProviderService>();
        var settings = settingsService.GetSetting("metaglm", model);
        return settings.ApiKey;
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
