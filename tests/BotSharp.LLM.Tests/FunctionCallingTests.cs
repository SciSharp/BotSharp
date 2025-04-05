using System.Text.Json;
using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.MLTasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BotSharp.Plugin.Google.Core
{
    public class FunctionCallingTests : TestBase
    {
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
                yield return new object[]
                {
                    services.BuildServiceProvider().GetService<IChatCompletion>() ??
                    throw new Exception("Error while initializing"),
                    agent, modelName
                };
            }

            if (LLMProvider.CanRunOpenAI)
            {
                //OpenAI
                (services, configuration, modelName) = LLMProvider.CreateOpenAI();
                yield return new object[]
                {
                    services.BuildServiceProvider().GetService<IChatCompletion>() ??
                    throw new Exception("Error while initializing"),
                    agent, modelName
                };
            }

            if (LLMProvider.CanRunAnthropic)
            {
                //Anthropic
                (services, configuration, modelName) = LLMProvider.CreateAnthropic();
                yield return new object[]
                {
                    services.BuildServiceProvider().GetService<IChatCompletion>() ??
                    throw new Exception("Error while initializing"),
                    agent, modelName
                };
            }
        }
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
                Disabled = false,
                Functions = new List<FunctionDef>([JsonSerializer.Deserialize<FunctionDef>("{\n  \"name\": \"get_weather_info\",\n  \"description\": \"get current weather info for a given city\",\n  \"parameters\": {\n    \"type\": \"object\",\n    \"properties\": {\n      \"city\": {\n        \"type\": \"string\",\n        \"description\": \"city name.\"\n      }\n    },\n    \"required\": [ \"city\" ]\n  }\n}")])
            };
        } 

        [Theory]
        [MemberData(nameof(CreateTestLLMProviders))]
        public async Task GetChatCompletions_Test(IChatCompletion chatCompletion, Agent agent, string modelName)
        {
            chatCompletion.SetModelName(modelName);
            var conversation =
                new List<RoleDialogModel>([new RoleDialogModel(AgentRole.User, "how's the weather in Sydney?")]);

            var result = await chatCompletion.GetChatCompletions(agent, conversation);
            result.FunctionName.ShouldBe("get_weather_info");
            result.FunctionArgs.ShouldContain("Sydney",Case.Insensitive);
        }

        [Theory]
        [MemberData(nameof(CreateTestLLMProviders))]
        public async Task GetChatCompletionsAsync_Test(IChatCompletion chatCompletion, Agent agent, string modelName)
        {
            chatCompletion.SetModelName(modelName);
            var conversation =
                new List<RoleDialogModel>([new RoleDialogModel(AgentRole.User, "how's the weather in Sydney?")]);
            RoleDialogModel reply = null;
            RoleDialogModel function = null;
            var result = await chatCompletion.GetChatCompletionsAsync(agent, conversation,
                async (received) => { reply = received; }, async (func) => { function = func; });
            result.ShouldBeTrue();
            function.ShouldNotBeNull();
            function.FunctionName.ShouldNotBeNullOrEmpty();
            function.FunctionArgs.ShouldContain("Sydney",Case.Insensitive);
        }

        [Theory]
        [MemberData(nameof(CreateTestLLMProviders))]
        public async Task GetChatCompletionsStreamingAsync_Test(IChatCompletion chatCompletion, Agent agent,
            string modelName)
        {
            //Not sure about support of function calling with Streaming
            
            
            // chatCompletion.SetModelName(modelName);
            // var conversation =
            //     new List<RoleDialogModel>([new RoleDialogModel(AgentRole.User, "how's the weather in Sydney?")]);
            // RoleDialogModel reply = null;
            // var result = await chatCompletion.GetChatCompletionsStreamingAsync(agent, conversation,
            //     async (received) => { reply = received; });
            // result.ShouldBeTrue();
            // reply.ShouldNotBeNull();
            // reply.Content.ShouldNotBeNullOrEmpty();
        }
    }
}