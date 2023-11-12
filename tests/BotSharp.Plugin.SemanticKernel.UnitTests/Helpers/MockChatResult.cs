using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Orchestration;

namespace BotSharp.Plugin.SemanticKernel.UnitTests.Helpers
{
    public class MockChatResult : IChatResult
    {
        public ModelResult ModelResult { get; set; }
        private string _response;

        public MockChatResult(string response)
        {
            ModelResult = new ModelResult(response);
            _response = response;
        }

        public async Task<ChatMessageBase> GetChatMessageAsync(CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(new MockModelResult(_response));
        }

        public class MockModelResult : ChatMessageBase
        {
            public MockModelResult(string content) : base(AuthorRole.Assistant, content, null)
            {
            }
        }
    }
}