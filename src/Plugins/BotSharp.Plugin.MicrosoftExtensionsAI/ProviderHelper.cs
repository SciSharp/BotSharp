using BotSharp.Abstraction.Conversations.Models;
using System.Collections.Generic;

namespace BotSharp.Plugin.MicrosoftExtensionsAI;

internal static class ProviderHelper
{
    public static IEnumerable<RoleDialogModel> GetChatSamples(List<string> lines)
    {
        foreach (string line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                int pos = line.IndexOf(' ');
                if (pos > 0)
                {
                    string role = line.Substring(0, pos - 1).Trim();
                    if (role != "##") // skip comments
                    {
                        yield return new RoleDialogModel(role, line.Substring(line.IndexOf(' ') + 1).Trim());
                    }
                }
            }
        }
    }
}
