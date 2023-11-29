using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Loggers;
using BotSharp.Abstraction.MLTasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BotSharp.Plugin.SemanticKernel
{
    /// <summary>
    /// Use Semantic Kernel as chat completion provider
    /// </summary>
    public class SemanticKernelChatCompletionProvider : IChatCompletion
    {
        private Microsoft.SemanticKernel.AI.ChatCompletion.IChatCompletion _kernelChatCompletion;
        private IServiceProvider _services;
        private ITokenStatistics _tokenStatistics;
        private string? _model = null;

        /// <inheritdoc/>
        public string Provider => "semantic-kernel";

        /// <summary>
        /// Create a new instance of <see cref="SemanticKernelChatCompletionProvider"/>
        /// </summary>
        /// <param name="chatCompletion"></param>
        /// <param name="services"></param>
        /// <param name="tokenStatistics"></param>
        public SemanticKernelChatCompletionProvider(Microsoft.SemanticKernel.AI.ChatCompletion.IChatCompletion chatCompletion,
            IServiceProvider services,
            ITokenStatistics tokenStatistics)
        {
            this._kernelChatCompletion = chatCompletion;
            this._services = services;
            this._tokenStatistics = tokenStatistics;
        }
        /// <inheritdoc/>
        public RoleDialogModel GetChatCompletions(Agent agent, List<RoleDialogModel> conversations)
        {
            var hooks = _services.GetServices<IContentGeneratingHook>().ToList();

            // Before chat completion hook
            Task.WaitAll(hooks.Select(hook =>
                hook.BeforeGenerating(agent, conversations)).ToArray());

            var completion = this._kernelChatCompletion;

            var agentService = _services.GetRequiredService<IAgentService>();
            var instruction = agentService.RenderedInstruction(agent);

            var chatHistory = completion.CreateNewChat(instruction);

            foreach (var message in conversations)
            {
                if (message.Role == AgentRole.User)
                {
                    chatHistory.AddUserMessage(message.Content);
                }
                else
                {
                    chatHistory.AddAssistantMessage(message.Content);
                }
            }

            var response = completion.GetChatCompletionsAsync(chatHistory)
                .ContinueWith(async t =>
                {
                    var result = await t;
                    var message = await result.First().GetChatMessageAsync();
                    return message.Content;
                }).ConfigureAwait(false).GetAwaiter().GetResult()
                .ConfigureAwait(false).GetAwaiter().GetResult();

            var msg = new RoleDialogModel(AgentRole.Assistant, response)
            {
                CurrentAgentId = agent.Id
            };

            // After chat completion hook
            Task.WaitAll(hooks.Select(hook =>
                hook.AfterGenerated(msg, new TokenStatsModel
                {
                    Model = _model ?? "default"
                })).ToArray());

            return msg;
        }
        /// <inheritdoc/>
        public Task<bool> GetChatCompletionsAsync(Agent agent, List<RoleDialogModel> conversations, Func<RoleDialogModel, Task> onMessageReceived, Func<RoleDialogModel, Task> onFunctionExecuting)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public Task<bool> GetChatCompletionsStreamingAsync(Agent agent, List<RoleDialogModel> conversations, Func<RoleDialogModel, Task> onMessageReceived)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public void SetModelName(string model)
        {
            if (!string.IsNullOrWhiteSpace(model))
                this._model = model;
        }
    }
}