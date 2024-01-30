using BotSharp.Abstraction.Conversations;
using BotSharp.Plugin.SemanticKernel.UnitTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace BotSharp.Plugin.SemanticKernel.Tests
{
    public class SemanticKernelTextCompletionProviderTests
    {
        private readonly IServiceProvider _services;
        private readonly ITokenStatistics _tokenStatistics;

        public SemanticKernelTextCompletionProviderTests()
        {
            
            _services = new ServiceCollection().BuildServiceProvider();
            _tokenStatistics = Mock.Of<ITokenStatistics>();
        }

        [Fact]
        public async Task GetCompletion_ReturnsExpectedResult()
        {
            // Arrange
           
            var text = "Hello";
            var expected = "Hello, world!";
            var provider = new SemanticKernelTextCompletionProvider(new SemanticKernelHelper(expected), _services, _tokenStatistics);

            // Act
            var result = await provider.GetCompletion(text, "agent1", "message1");

            // Assert
            Assert.Equal(expected, result);

        }
    }
}
