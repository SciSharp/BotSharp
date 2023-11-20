using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.VectorStorage;
using BotSharp.Plugin.SemanticKernel.UnitTests.Helpers;
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
            services.AddScoped(x => Mock.Of<Microsoft.SemanticKernel.AI.TextCompletion.ITextCompletion>());
            services.AddScoped(x => Mock.Of<Microsoft.SemanticKernel.AI.ChatCompletion.IChatCompletion>());
            services.AddScoped(x => Mock.Of<Microsoft.SemanticKernel.Memory.IMemoryStore>());
            services.AddScoped(x => Mock.Of<Microsoft.SemanticKernel.AI.Embeddings.ITextEmbeddingGeneration>());
            services.AddScoped(x => Mock.Of<ITokenStatistics>());


            plugin.RegisterDI(services, config);

            var provider = services.BuildServiceProvider().CreateScope().ServiceProvider;

            Assert.NotNull(provider.GetService<ITextCompletion>());
            Assert.NotNull(provider.GetService<IChatCompletion>());
            Assert.NotNull(provider.GetService<IVectorDb>());
            Assert.NotNull(provider.GetService<ITextEmbedding>());
        }
    }
}