using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.MLTasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Shouldly;

namespace BotSharp.Plugin.Google.Core
{
    public class ChatCompletionTests:TestBase
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
                yield return new object[] { services.BuildServiceProvider().GetService<IChatCompletion>() ?? throw new Exception("Error while initializing"), agent, modelName };
            }

            if (LLMProvider.CanRunOpenAI)
            {
                //OpenAI
                (services, configuration, modelName) = LLMProvider.CreateOpenAI();
                yield return new object[] { services.BuildServiceProvider().GetService<IChatCompletion>() ?? throw new Exception("Error while initializing"), agent, modelName };
            }

            if (LLMProvider.CanRunAnthropic)
            {
                //Anthropic
                (services, configuration, modelName) = LLMProvider.CreateAnthropic();
                yield return new object[] { services.BuildServiceProvider().GetService<IChatCompletion>() ?? throw new Exception("Error while initializing"), agent, modelName };
            }
        }
        public ChatCompletionTests()
        {
           
        }

        [Theory]
        [MemberData(nameof(CreateTestLLMProviders))]
        public async Task GetChatCompletions_Test(IChatCompletion chatCompletion, Agent agent, string modelName)
        {
            chatCompletion.SetModelName(modelName);
            var conversation = new List<RoleDialogModel>([new RoleDialogModel(AgentRole.User, "write a poem about stars")]);
            
            var result = await chatCompletion.GetChatCompletions(agent,conversation);
            result.Content.ShouldNotBeNullOrEmpty();
        }
        
        [Theory]
        [MemberData(nameof(CreateTestLLMProviders))]
        public async Task GetChatCompletionsAsync_Test(IChatCompletion chatCompletion, Agent agent, string modelName)
        {
            chatCompletion.SetModelName(modelName);
            var conversation = new List<RoleDialogModel>([new RoleDialogModel(AgentRole.User, "write a poem about stars")]);
            RoleDialogModel reply = null;
            var result = await chatCompletion.GetChatCompletionsAsync(agent,conversation, async (received) =>
            {
                reply = received;
            }, async (func) =>
            {
                
            });
            result.ShouldBeTrue();
            reply.ShouldNotBeNull();
            reply.Content.ShouldNotBeNullOrEmpty();
        }
        
        [Theory]
        [MemberData(nameof(CreateTestLLMProviders))]
        public async Task GetChatCompletionsStreamingAsync_Test(IChatCompletion chatCompletion, Agent agent, string modelName)
        {
            chatCompletion.SetModelName(modelName);
            var conversation = new List<RoleDialogModel>([new RoleDialogModel(AgentRole.User, "write a poem about stars")]);
            RoleDialogModel reply = null;
            var result = await chatCompletion.GetChatCompletionsStreamingAsync(agent,conversation, async (received) =>
            {
                reply = received;
            });
            result.ShouldBeTrue();
            reply.ShouldNotBeNull();
            reply.Content.ShouldNotBeNullOrEmpty();
        }
    }
}