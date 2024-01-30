using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Services;
using Microsoft.SemanticKernel.TextGeneration;

namespace BotSharp.Plugin.SemanticKernel.UnitTests.Helpers
{
    internal class SemanticKernelHelper : IChatCompletionService, ITextGenerationService, IAIService
    {
        private Dictionary<string, string> _attributes = new();
        private readonly string _excepted;

        public SemanticKernelHelper(string excepted)
        {
            this._excepted = excepted;
        }

        public IReadOnlyDictionary<string, string> Attributes => _attributes;

        IReadOnlyDictionary<string, object?> IAIService.Attributes => throw new NotImplementedException();

        public ChatHistory CreateNewChat(string? instructions = null)
        {
            return new ChatHistory();
        }

        public Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(ChatHistory chatHistory, PromptExecutionSettings? executionSettings = null, Kernel? kernel = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<ChatMessageContent>>(new List<ChatMessageContent> { new ChatMessageContent(AuthorRole.Assistant, _excepted) });

        }

        public IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(ChatHistory chatHistory, PromptExecutionSettings? executionSettings = null, Kernel? kernel = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<TextContent>> GetTextContentsAsync(string prompt, PromptExecutionSettings? executionSettings = null, Kernel? kernel = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<TextContent>>(new List<TextContent> { new TextContent(_excepted) });
        }

        public IAsyncEnumerable<StreamingTextContent> GetStreamingTextContentsAsync(string prompt, PromptExecutionSettings? executionSettings = null, Kernel? kernel = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
