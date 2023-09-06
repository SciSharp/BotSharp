using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Conversations.Settings;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Plugins.LLamaSharp;
using LLama;
using LLama.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BotSharp.Plugin.LLamaSharp.Providers;

public class ChatCompletionProvider : IChatCompletion
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;

    public ChatCompletionProvider(IServiceProvider services,
        ILogger<ChatCompletionProvider> logger)
    {
        _services = services;
        _logger = logger;
    }

    public string GetChatCompletions(Agent agent, List<RoleDialogModel> conversations, Func<RoleDialogModel, Task> onMessageReceived)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> GetChatCompletionsAsync(Agent agent,
        List<RoleDialogModel> conversations,
        Func<RoleDialogModel, Task> onMessageReceived,
        Func<RoleDialogModel, Task> onFunctionExecuting)
    {
        var content = string.Join("\n", conversations.Select(x => $"{x.Role}: {x.Content}")).Trim();
        content += $"\n{AgentRole.Assistant}: ";

        var llama = _services.GetRequiredService<LlamaAiModel>();
        llama.LoadModel();
        var executor = llama.GetStatelessExecutor();

        var inferenceParams = new InferenceParams()
        {
            Temperature = 1.0f,
            AntiPrompts = new List<string> { $"{AgentRole.User}:", "\n", "?" },
            MaxTokens = 256
        };

        string totalResponse = "";

        var prompt = agent.Instruction + content;

        var convSetting = _services.GetRequiredService<ConversationSetting>();
        if (convSetting.ShowVerboseLog)
        {
            _logger.LogInformation(prompt);
        }

        foreach (var response in executor.Infer(prompt, inferenceParams))
        {
            Console.Write(response);
            totalResponse += response;
        }

        foreach (var anti in inferenceParams.AntiPrompts)
        {
            totalResponse = totalResponse.Replace(anti, "").Trim();
        }

        await onMessageReceived(new RoleDialogModel(AgentRole.Assistant, totalResponse));

        return true;
    }

    public async Task<bool> GetChatCompletionsStreamingAsync(Agent agent, List<RoleDialogModel> conversations, Func<RoleDialogModel, Task> onMessageReceived)
    {
        string totalResponse = "";
        var content = string.Join("\n", conversations.Select(x => $"{x.Role}: {x.Content}")).Trim();
        content += $"\n{AgentRole.Assistant}: ";

        var llama = _services.GetRequiredService<LlamaAiModel>();
        llama.LoadModel();
        var executor = new StatelessExecutor(llama.Model, llama.Params);
        var inferenceParams = new InferenceParams() { Temperature = 1.0f, AntiPrompts = new List<string> { $"{AgentRole.User}:" }, MaxTokens = 64 };

        var convSetting = _services.GetRequiredService<ConversationSetting>();
        if (convSetting.ShowVerboseLog)
        {
            _logger.LogInformation(agent.Instruction);
        }

        foreach (var response in executor.Infer(agent.Instruction, inferenceParams))
        {
            Console.Write(response);
            totalResponse += response;
        }

        return true;
    }
}
