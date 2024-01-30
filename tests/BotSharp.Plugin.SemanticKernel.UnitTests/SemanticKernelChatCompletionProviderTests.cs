using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Loggers;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Moq;

namespace BotSharp.Plugin.SemanticKernel.Tests
{
    public class SemanticKernelChatCompletionProviderTests
    {
        private readonly Mock<IChatCompletionService> _chatCompletionMock;
        private readonly Mock<IServiceProvider> _servicesMock;
        private readonly Mock<ITokenStatistics> _tokenStatisticsMock;
        private readonly SemanticKernelChatCompletionProvider _provider;

        public SemanticKernelChatCompletionProviderTests()
        {
            _chatCompletionMock = new Mock<IChatCompletionService>();
            _servicesMock = new Mock<IServiceProvider>();
            _tokenStatisticsMock = new Mock<ITokenStatistics>();
            _provider = new SemanticKernelChatCompletionProvider(_chatCompletionMock.Object, _servicesMock.Object, _tokenStatisticsMock.Object);
        }

        [Fact]
        public async void GetChatCompletions_Returns_RoleDialogModel()
        {
            // Arrange
            var agent = new Agent();
            var conversations = new List<RoleDialogModel>
            {
                new RoleDialogModel(AgentRole.User, "Hello")
            };

            _servicesMock.Setup(x => x.GetService(typeof(IEnumerable<IContentGeneratingHook>)))
                        .Returns(new List<IContentGeneratingHook>());
            var agentService = new Mock<IAgentService>();
            agentService.Setup(x => x.RenderedInstruction(agent)).Returns("How can I help you?");
            _servicesMock.Setup(x => x.GetService(typeof(IAgentService)))
                .Returns(agentService.Object);

            var chatHistoryMock = new Mock<ChatHistory>();
            //_chatCompletionMock.Setup(x => new ChatHistory(It.IsAny<string>())).Returns(chatHistoryMock.Object);
    
            _chatCompletionMock.Setup(x => x.GetChatMessageContentsAsync(chatHistoryMock.Object, It.IsAny<PromptExecutionSettings>(), It.IsAny<Kernel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ChatMessageContent>
                {
                    new ChatMessageContent(AuthorRole.Assistant,"How can I help you?")
                });

            // Act
            var result = await _provider.GetChatCompletions(agent, conversations);

            // Assert
            Assert.IsType<RoleDialogModel>(result);
        }
    }

}
