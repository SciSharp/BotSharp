using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.Realtime.Models;
using GenerativeAI;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BotSharp.Plugin.Google.Core
{
    public class GoogleRealTimeTests : TestBase
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

        [Fact]
        public async Task ShouldConnect_Tests()
        {
            if (!LLMProvider.CanRunGemini)
                return;
            
            (IServiceCollection services, IConfiguration config, string modelName) = LLMProvider.CreateGemini();

            var agent = CreateTestAgent();
            var realTimeCompleter = services.BuildServiceProvider().GetService<IRealTimeCompletion>();
            realTimeCompleter.SetModelName(GoogleAIModels.Gemini2FlashExp);
            bool modelReady = false;
            await realTimeCompleter.Connect(new RealtimeHubConnection(), () => { modelReady = true; },
                (s, s1) => { Console.WriteLine(s); }, () => { }, (s) => { Console.WriteLine(s); },
                (list => { Console.WriteLine(list); }),
                (s => { Console.WriteLine(s); }),
                (model => { Console.WriteLine(model); }), (() => { Console.WriteLine("UserInterrupted"); }));
            Thread.Sleep(1000);
            modelReady.ShouldBeTrue();

            await realTimeCompleter.InsertConversationItem(new RoleDialogModel(AgentRole.User,
                "tell me something about Albert Einstein."));
            Thread.Sleep(10000);
        }
    }
}