using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Plugin.SemanticKernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Moq;
using Xunit;
using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Agents.Models;
using System.Linq;
using System.Runtime;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Models;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.AzureSdk;
using BotSharp.Plugin.SemanticKernel.UnitTests.Helpers;

namespace BotSharp.Plugin.SemanticKernel.Tests
{
    public class SemanticKernelChatCompletionProviderTests
    {
        private readonly Mock<Microsoft.SemanticKernel.AI.ChatCompletion.IChatCompletion> _chatCompletionMock;
        private readonly Mock<IServiceProvider> _servicesMock;
        private readonly Mock<ITokenStatistics> _tokenStatisticsMock;
        private readonly SemanticKernelChatCompletionProvider _provider;

        public SemanticKernelChatCompletionProviderTests()
        {
            _chatCompletionMock = new Mock<Microsoft.SemanticKernel.AI.ChatCompletion.IChatCompletion>();
            _servicesMock = new Mock<IServiceProvider>();
            _tokenStatisticsMock = new Mock<ITokenStatistics>();
            _provider = new SemanticKernelChatCompletionProvider(_chatCompletionMock, _servicesMock.Object, _tokenStatisticsMock.Object);
        }

        [Fact]
        public void GetChatCompletions_Returns_RoleDialogModel()
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
            agentService.Setup(x => x.RenderedInstruction(agent)).Returns("");
            _servicesMock.Setup(x => x.GetService(typeof(IAgentService)))
                .Returns(agentService.Object);

            var chatHistoryMock = new Mock<ChatHistory>();
            var chatCompletionMock = new Mock<Microsoft.SemanticKernel.AI.ChatCompletion.IChatCompletion>();
            chatCompletionMock.Setup(x => x.CreateNewChat(It.IsAny<string>())).Returns(chatHistoryMock.Object);
            chatCompletionMock.Setup(x => x.GetChatCompletionsAsync(chatHistoryMock.Object, It.IsAny<AIRequestSettings>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<IChatResult>
            {
                new ResultHelper("How can I help you?")
            });

            _kernelMock.Setup(x => x.GetService<Microsoft.SemanticKernel.AI.ChatCompletion.IChatCompletion>(null)).Returns(chatCompletionMock.Object);

            // Act
            var result = _provider.GetChatCompletions(agent, conversations);

            // Assert
            Assert.IsType<RoleDialogModel>(result);
        }
    }

}
