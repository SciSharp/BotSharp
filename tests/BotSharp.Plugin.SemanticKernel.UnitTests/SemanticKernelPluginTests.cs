using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.MLTasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Moq;

namespace BotSharp.Plugin.SemanticKernel.Tests
{
    public class SemanticKernelPluginTests
    {
        [Fact]
        public void TestRegisterDI()
        {
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder().Build();
            var plugin = new SemanticKernelPlugin();
            services.AddScoped(x =>
            {
                return new KernelBuilder()
                .WithAzureOpenAIChatCompletionService("test", "test", "test")
                .Build();
            });
            services.AddScoped<ITokenStatistics>(x=> Mock.Of<ITokenStatistics>());


            plugin.RegisterDI(services, config);

            var provider = services.BuildServiceProvider();

            Assert.NotNull(provider.GetService<ITextCompletion>());
            Assert.NotNull(provider.GetService<IChatCompletion>());
        }
    }
}