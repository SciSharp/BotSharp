using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.MLTasks;
using LLama;
using System.IO;

namespace BotSharp.Core.Plugins.LLamaSharp;

public class ChatCompletionProvider : IChatCompletion
{
    private IChatModel _model;


    public ChatCompletionProvider(LlamaAiModel model)
    {
        model.LoadModel();
        _model = model.Model;
        // _model.InitChatPrompt(prompt, "UTF-8");
        // _model.InitChatAntiprompt(new string[] { "user:" });
    }

    public async Task GetChatCompletionsAsync(List<RoleDialogModel> conversations,
        Func<string, Task> onChunkReceived)
    {
        string totalResponse = "";
        var content = string.Join("\n ", conversations.Select(x => $"{x.Role}: {x.Text.Replace("user:", "")}")).Trim();
        content += "\n assistant: ";
        foreach (var response in _model.Chat(content, "", "UTF-8"))
        {
            Console.Write(response);
            totalResponse += response;
            await onChunkReceived(response);
        }

        Console.WriteLine();
    }

    public Task<string> GetChatCompletionsAsync(Agent agent, List<RoleDialogModel> conversations)
    {
        string totalResponse = "";
        var content = string.Join("\n", conversations.Select(x => $"{x.Role}: {x.Text.Replace("user:", "")}")).Trim();
        content += "\nassistant: ";
        foreach (var response in _model.Chat(content, agent.Instruction, "UTF-8"))
        {
            if (response == "\n")
            {
                break;
            }
            Console.Write(response);
            totalResponse += response;
        }

        return Task.FromResult(totalResponse.Trim());
    }
}
