using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

//namespace BotSharp.Plugin.SemanticKernel.UnitTests.Helpers
//{
//    public class ResultHelper : KernelContent
//    {
//        public TextContent ModelResult { get; set; }
//        private string _response;

//        public ResultHelper(string response)
//        {
//            ModelResult = new TextContent(response);
//            _response = response;
//        }

//        public async Task<ChatMessageContent> GetChatMessageAsync(CancellationToken cancellationToken = default)
//        {
//            return await Task.FromResult(new MockModelResult(_response));
//        }

//        public Task<string> GetCompletionAsync(CancellationToken cancellationToken = default)
//        {
//            return Task.FromResult(_response);
//        }

//        public class MockModelResult : ChatMessageContent
//        {
//            public MockModelResult(string content) : base(AuthorRole.Assistant, content, null)
//            {
//            }
//        }
//    }
//}