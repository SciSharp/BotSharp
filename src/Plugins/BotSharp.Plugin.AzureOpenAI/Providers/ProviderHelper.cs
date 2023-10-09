using Azure.AI.OpenAI;
using Azure;
using System;
using BotSharp.Plugin.AzureOpenAI.Settings;
using BotSharp.Abstraction.Conversations.Models;
using System.Collections.Generic;

namespace BotSharp.Plugin.AzureOpenAI.Providers;

public class ProviderHelper
{
    public static (OpenAIClient, string) GetClient(string model, AzureOpenAiSettings settings)
    {
        if (model == "gpt-4")
        {
            var client = new OpenAIClient(new Uri(settings.GPT4.Endpoint), new AzureKeyCredential(settings.GPT4.ApiKey));
            return (client, settings.GPT4.DeploymentModel);
        }
        else
        {
            var client = new OpenAIClient(new Uri(settings.Endpoint), new AzureKeyCredential(settings.ApiKey));
            return (client, settings.DeploymentModel.ChatCompletionModel);
        }
    }

    public static List<RoleDialogModel> GetChatSamples(string sampleText)
    {
        var samples = new List<RoleDialogModel>();
        if (string.IsNullOrEmpty(sampleText))
        {
            return samples;
        }

        var lines = sampleText.Split('\n');
        for (int i = 0; i < lines.Length; i++)
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
