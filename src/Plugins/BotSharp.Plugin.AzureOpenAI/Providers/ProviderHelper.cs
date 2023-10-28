using Azure.AI.OpenAI;
using Azure;
using System;
using BotSharp.Plugin.AzureOpenAI.Settings;
using BotSharp.Abstraction.Conversations.Models;
using System.Collections.Generic;

namespace BotSharp.Plugin.AzureOpenAI.Providers;

public class ProviderHelper
{
    public static OpenAIClient GetClient(string model, AzureOpenAiSettings settings)
    {
        if (model == "gpt-4" || model == "llm-gpt4")
        {
            var client = new OpenAIClient(new Uri(settings.GPT4.Endpoint), new AzureKeyCredential(settings.GPT4.ApiKey));
            return client;
        }
        else
        {
            var client = new OpenAIClient(new Uri(settings.Endpoint), new AzureKeyCredential(settings.ApiKey));
            return client;
        }
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
