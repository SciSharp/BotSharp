using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.AI.TextCompletion;
using Microsoft.SemanticKernel.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Plugin.SemanticKernel.UnitTests.Helpers
{
    internal class SemanticKernelHelper : IChatCompletion, ITextCompletion, IAIService
    {
        private readonly string _excepted;

        public SemanticKernelHelper(string excepted)
        {
            this._excepted = excepted;
        }

        public ChatHistory CreateNewChat(string? instructions = null)
        {
            return new ChatHistory();
        }

        public Task<IReadOnlyList<IChatResult>> GetChatCompletionsAsync(ChatHistory chat, AIRequestSettings? requestSettings = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<IChatResult>>( new List<IChatResult> { new ResultHelper(_excepted) });
        }

        public Task<IReadOnlyList<ITextResult>> GetCompletionsAsync(string text, AIRequestSettings? requestSettings = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<ITextResult>>(new List<ITextResult> { new ResultHelper(_excepted) });
        }

        public IAsyncEnumerable<IChatStreamingResult> GetStreamingChatCompletionsAsync(ChatHistory chat, AIRequestSettings? requestSettings = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<ITextStreamingResult> GetStreamingCompletionsAsync(string text, AIRequestSettings? requestSettings = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
