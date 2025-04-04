

using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.MLTasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BotSharp.Plugin.Google.Core
{
    public class EmbeddingTests:TestBase
    {
        protected static Agent CreateTestAgent()
        {
            return new Agent()
            {
                Id = "test-agent-id",
                Name = "TestAgent",
                Description = "This is a test agent used for unit testing purposes.",
                Type = "Chat",
                CreatedDateTime = DateTime.UtcNow,
                UpdatedDateTime = DateTime.UtcNow,
                IsPublic = false,
                Disabled = false
            };
        }
        public static IEnumerable<object[]> CreateTestLLMProviders()
        {
            //Common
            var agent = CreateTestAgent();
            IServiceCollection services;
            IConfiguration configuration; 
            string modelName;

            if (LLMProvider.CanRunGemini)
            {
                //Google Gemini
                (services, configuration, modelName) = LLMProvider.CreateGemini();
                yield return new object[] { services.BuildServiceProvider().GetService<ITextEmbedding>() ?? throw new Exception("Error while initializing"), agent, modelName };
            }

            if (LLMProvider.CanRunOpenAI)
            {
                //OpenAI
                (services, configuration, modelName) = LLMProvider.CreateOpenAI();
                yield return new object[] { services.BuildServiceProvider().GetService<ITextEmbedding>() ?? throw new Exception("Error while initializing"), agent, modelName };
            }
        }
       

        [Theory]
        [MemberData(nameof(CreateTestLLMProviders))]
        public async Task GetChatCompletions_Test(ITextEmbedding chatCompletion, Agent agent, string modelName)
        {
            var text = "This is a placeholder for a really long text used for testing, generated for simulation purposes. The text simulates a verbose input and can be modified to any required content.";
            
            var result = await chatCompletion.GetVectorAsync(text);
            result.ShouldNotBeEmpty();
        }
       
    }
}