using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.AI.TextCompletion;
using Microsoft.SemanticKernel.Orchestration;

namespace BotSharp.Plugin.SemanticKernel.UnitTests.Helpers
{
    public class ResultHelper : IChatResult, ITextResult
    {
        public ModelResult ModelResult { get; set; }
        private string _response;

        public ResultHelper(string response)
        {
            ModelResult = new ModelResult(response);
            _response = response;
        }

        public async Task<ChatMessage> GetChatMessageAsync(CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(new MockModelResult(_response));
        }

        public Task<string> GetCompletionAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_response);
        }

        public class MockModelResult : ChatMessage
        {
            public MockModelResult(string content) : base(AuthorRole.Assistant, content, null)
            {
            }
        }
    }
}