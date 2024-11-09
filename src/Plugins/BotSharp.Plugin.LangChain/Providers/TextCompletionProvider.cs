using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Loggers;
using BotSharp.Abstraction.MLTasks;
using LangChain.Providers;
using LangChain.Providers.Google.VertexAI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BotSharp.Plugin.VertexAI.Providers
{
    public class TextCompletionProvider(
    VertexAIConfiguration config,
    ChatSettings settings,
    ILogger<TextCompletionProvider> logger,
    IServiceProvider services) : ITextCompletion
    {
        public string Provider => "vertexai";
        private readonly VertexAIConfiguration _config = config;
        private readonly ChatSettings? _settings = settings;
        private readonly IServiceProvider _services = services;
        private readonly ILogger _logger = logger;
        public required string _model;

        public async Task<string> GetCompletion(string text, string agentId, string messageId)
        {
            var contentHooks = _services.GetServices<IContentGeneratingHook>().ToList();
            var agent = new Agent()
            {
                Id = agentId,
            };

            var client = new VertexAIProvider(_config);
            var model = new VertexAIChatModel(client, _model);
            var response = await model.GenerateAsync(text, _settings);

            var responseMessage = new RoleDialogModel(AgentRole.Assistant, response.LastMessageContent)
            {
                CurrentAgentId = agentId,
                MessageId = messageId
            };

            Task.WaitAll(contentHooks.Select(hook =>
            hook.AfterGenerated(responseMessage, new TokenStatsModel
            {
                Prompt = text,
                Provider = Provider,
                Model = _model,
                PromptCount = response.Usage.TotalTokens,
                CompletionCount = response.Usage.OutputTokens
            })).ToArray());

            return response.LastMessageContent;
        }

        public void SetModelName(string model)
        {
            _model = model;
        }
    }
}
