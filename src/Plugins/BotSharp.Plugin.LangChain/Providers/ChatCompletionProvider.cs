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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BotSharp.Plugin.VertexAI.Providers
{
    public class ChatCompletionProvider(
    VertexAIConfiguration config,
    ChatSettings settings,
    ILogger<TextCompletionProvider> logger,
    IServiceProvider services) : IChatCompletion
    {
        public string Provider => "vertexai";
        private readonly VertexAIConfiguration _config = config;
        private readonly ChatSettings? _settings = settings;
        private readonly IServiceProvider _services = services;
        private readonly ILogger _logger = logger;
        public required string _model;

        public void SetModelName(string model)
        {
            _model = model;
        }

        public async Task<RoleDialogModel> GetChatCompletions(Agent agent, List<RoleDialogModel> conversations)
        {
            var hooks = _services.GetServices<IContentGeneratingHook>().ToList();
            Task.WaitAll(hooks.Select(hook =>
            hook.BeforeGenerating(agent, conversations)).ToArray());
            var client = new VertexAIProvider(_config);
            var model = new VertexAIChatModel(client, _model);
            var messages = conversations
                .Select(c => new Message(c.Content, c.Role == AgentRole.User ? MessageRole.Human : MessageRole.Ai)).ToList();

            var response = await model.GenerateAsync(new ChatRequest { Messages = messages }, _settings);

           var msg = new RoleDialogModel(MessageRole.Ai.ToString(), response.LastMessageContent)
            {
                CurrentAgentId = agent.Id
            };

            Task.WaitAll(hooks.Select(hook =>
            hook.AfterGenerated(msg, new TokenStatsModel
            {
                Prompt = response.Messages[0].Content,
                Model = _model
            })).ToArray());

            return msg;
        }

        public Task<bool> GetChatCompletionsAsync(Agent agent, List<RoleDialogModel> conversations, Func<RoleDialogModel, Task> onMessageReceived, Func<RoleDialogModel, Task> onFunctionExecuting)
        {
            throw new NotImplementedException();
        }

        public Task<bool> GetChatCompletionsStreamingAsync(Agent agent, List<RoleDialogModel> conversations, Func<RoleDialogModel, Task> onMessageReceived)
        {
            throw new NotImplementedException();
        }
    }
}
