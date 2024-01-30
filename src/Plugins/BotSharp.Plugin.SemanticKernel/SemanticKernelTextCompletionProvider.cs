using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Loggers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BotSharp.Plugin.SemanticKernel
{
    /// <summary>
    /// User Semantic Kernel as text completion provider
    /// </summary>
    public class SemanticKernelTextCompletionProvider : Abstraction.MLTasks.ITextCompletion
    {
        private readonly Microsoft.SemanticKernel.TextGeneration.ITextGenerationService _kernelTextCompletion;
        private readonly IServiceProvider _services;
        private readonly ITokenStatistics _tokenStatistics;
        private string? _model = null;

        /// <inheritdoc/>
        public string Provider => "semantic-kernel";

        /// <summary>
        /// Create a new instance of <see cref="SemanticKernelTextCompletionProvider"/>
        /// </summary>
        /// <param name="textCompletion"></param>
        /// <param name="services"></param>
        /// <param name="tokenStatistics"></param>
        public SemanticKernelTextCompletionProvider(Microsoft.SemanticKernel.TextGeneration.ITextGenerationService textCompletion,
            IServiceProvider services,
            ITokenStatistics tokenStatistics)
        {
            this._kernelTextCompletion = textCompletion;
            this._services = services;
            this._tokenStatistics = tokenStatistics;
        }

        /// <inheritdoc/>
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

            var completion = this._kernelTextCompletion;
            _tokenStatistics.StartTimer();
            var textContent = await completion.GetTextContentsAsync(text);
            var result = textContent.FirstOrDefault().Text;
            _tokenStatistics.StopTimer();

           
            // After chat completion hook
            Task.WaitAll(hooks.Select(hook =>
                hook.AfterGenerated(new RoleDialogModel(AgentRole.Assistant, result), new TokenStatsModel
                {
                    Model = _model ?? "default"
                })).ToArray());

            return result;
        }

        /// <inheritdoc/>
        public void SetModelName(string model)
        {
            if (!string.IsNullOrWhiteSpace(model))
                this._model = model;
        }
    }
}