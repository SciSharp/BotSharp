using Microsoft.Extensions.DependencyInjection;
using BotSharp.Abstraction.Conversations;

namespace UnitTest
{    
    [TestClass]
    public class MainTest
    {
        [TestMethod]
        public void TestConversationHookProvider()
        {
            var services = new ServiceCollection();

            services.AddSingleton<IConversationHook, TestHookC>();
            services.AddSingleton<IConversationHook, TestHookA>();
            services.AddSingleton<IConversationHook, TestHookB>();
            
            services.AddSingleton<ConversationHookProvider>();

            var serviceProvider = services.BuildServiceProvider();
            var conversationHookProvider = serviceProvider.GetService<ConversationHookProvider>();

            Assert.AreEqual(3, conversationHookProvider.Hooks.Count());

            var prevHook = default(IConversationHook);

            // Assert priority
            foreach (var hook in conversationHookProvider.HooksOrderByPriority)
            {
                if (prevHook != null)
                {
                    Assert.IsTrue(prevHook.Priority < hook.Priority);
                }

                prevHook = hook;
            }
        }

        class TestHookA : ConversationHookBase
        {
            public TestHookA()
            {
                Priority = 1;
            }
        }

        class TestHookB : ConversationHookBase
        {
            public TestHookA()
            {
                Priority = 2;
            }
        }

        class TestHookC : ConversationHookBase
        {
            public TestHookA()
            {
                Priority = 3;
            }
        }
    }
}