using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Plugin.SemanticKernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.TextCompletion;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Conversations;
using System;
using System.Linq;
using Microsoft;
using Microsoft.SemanticKernel.AI;

namespace BotSharp.Plugin.SemanticKernel.Tests
{
    public class SemanticKernelTextCompletionProviderTests
    {
        private readonly Mock<IKernel> _kernel;
        private readonly IServiceProvider _services;
        private readonly ITokenStatistics _tokenStatistics;

        public SemanticKernelTextCompletionProviderTests()
        {
            _kernel = new Mock<IKernel>();
            _services = new ServiceCollection().BuildServiceProvider();
            _tokenStatistics = Mock.Of<ITokenStatistics>();
        }

        [Fact]
        public async Task GetCompletion_ReturnsExpectedResult()
        {
            // Arrange
            var provider = new SemanticKernelTextCompletionProvider(_kernel.Object, _services, _tokenStatistics);
            var text = "Hello";
            var agentId = "agent1";
            var messageId = "message1";
            var expected = "Hello, world!";

            var mockCompletion = new Mock<Microsoft.SemanticKernel.AI.TextCompletion.ITextCompletion>();
            mockCompletion.Setup(c => c.CompleteAsync(text, It.IsAny<AIRequestSettings>(), It.IsAny<CancellationToken>())).ReturnsAsync(expected);
            _kernel.Setup(c => c.GetService<Microsoft.SemanticKernel.AI.TextCompletion.ITextCompletion>(It.IsAny<string>())).Returns(mockCompletion.Object);

            // Act
            var result = await provider.GetCompletion(text, agentId, messageId);

            // Assert
            Assert.Equal(expected, result);
            mockCompletion.Verify(c => c.CompleteAsync(text, null, default(CancellationToken)), Times.Once);

        }
    }
}
