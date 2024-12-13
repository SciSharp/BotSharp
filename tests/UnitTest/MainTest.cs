using Microsoft.Extensions.DependencyInjection;
using BotSharp.Abstraction.Conversations;
using BotSharp.Core.Evaluations;
using BotSharp.Plugin.ChatHub.Hooks;

namespace UnitTest
{    
    [TestClass]
    public class MainTest
    {
        [TestMethod]
        public void TestConversationHookProvider()
        {
            var services = new ServiceCollection();

            services.AddSingleton<IConversationHook, EvaluationConversationHook>();
            services.AddSingleton<IConversationHook, WelcomeHook>();
            services.AddSingleton<IConversationHook, ChatHubConversationHook>();
            services.AddSingleton<ConversationHookProvider>();

            var serviceProvider = services.BuildServiceProvider();
            var conversationHookProvider = serviceProvider.GetService<ConversationHookProvider>();
            
            Assert.AreEqual(3, conversationHookProvider.Hooks.Count());

            // ChatHubConversationHook has the top priority
            Assert.IsInstanceOfType<ChatHubConversationHook>(conversationHookProvider.HooksOrderByPriority.FirstOrDefault());
        }
    }
}