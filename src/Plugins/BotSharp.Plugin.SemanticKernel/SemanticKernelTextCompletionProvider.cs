using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.MLTasks;
using Microsoft.SemanticKernel;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Collections.Generic;
using Microsoft.SemanticKernel.AI.TextCompletion;

namespace BotSharp.Plugin.SemanticKernel
{
    public class SemanticKernelTextCompletionProvider : Abstraction.MLTasks.ITextCompletion
    {
        private readonly IKernel _kernel;
        private readonly IServiceProvider _services;
        private readonly ITokenStatistics _tokenStatistics;
        private string? _model = null;

        public string Provider => "semantic-kernel";

        public SemanticKernelTextCompletionProvider(IKernel kernel,
            IServiceProvider services,
            ITokenStatistics tokenStatistics)
        {
            this._kernel = kernel;
            this._services = services;
            this._tokenStatistics = tokenStatistics;
        }


        public async Task<string> GetCompletion(string text, string agentId, string messageId)
        {
            var hooks = _services.GetServices<IContentGeneratingHook>().ToList();

            // Before chat completion hook
            var agent = new Agent()
            {
                Id = agentId
            };
            var userMessage = new RoleDialogModel(AgentRole.User, text)
            {
                MessageId = messageId
            };
            Task.WaitAll(hooks.Select(hook =>
                hook.BeforeGenerating(agent, new List<RoleDialogModel> { userMessage })).ToArray());

            var completion = _kernel.GetService<Microsoft.SemanticKernel.AI.TextCompletion.ITextCompletion>(_model);
            _tokenStatistics.StartTimer();
            var result = await completion.CompleteAsync(text);
            _tokenStatistics.StopTimer();

            // After chat completion hook
            Task.WaitAll(hooks.Select(hook =>
                hook.AfterGenerated(new RoleDialogModel(AgentRole.Assistant, result), new TokenStatsModel
                {
                    Model = _model ?? "default"
                })).ToArray());

            return result;
        }

        public void SetModelName(string model)
        {
            this._model = model;
        }
    }
}
