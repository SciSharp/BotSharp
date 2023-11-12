using BotSharp.Abstraction.MLTasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

            plugin.RegisterDI(services, config);

            var provider = services.BuildServiceProvider();

            Assert.NotNull(provider.GetService<ITextCompletion>());
            Assert.NotNull(provider.GetService<IChatCompletion>());
        }
    }
}