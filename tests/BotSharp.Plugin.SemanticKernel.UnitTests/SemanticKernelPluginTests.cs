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

            services.AddSingleton<IConfiguration>(config);

            var plugin = new SemanticKernelPlugin();
            services.AddScoped(x => Mock.Of<Microsoft.SemanticKernel.TextGeneration.ITextGenerationService>());
            services.AddScoped(x => Mock.Of<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>());
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            services.AddScoped(x => Mock.Of<Microsoft.SemanticKernel.Memory.IMemoryStore>());
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            services.AddScoped(x => Mock.Of<Microsoft.SemanticKernel.Embeddings.ITextEmbeddingGenerationService>());
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
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