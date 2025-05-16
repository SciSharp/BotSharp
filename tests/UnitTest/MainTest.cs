using Microsoft.Extensions.DependencyInjection;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Hooks;

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

            var serviceProvider = services.BuildServiceProvider();
            var hooks = serviceProvider.GetHooksOrderByPriority<IConversationHook>(string.Empty);

            Assert.AreEqual(3, hooks.Count());

            var prevHook = default(IConversationHook);

            // Assert priority
            foreach (var hook in hooks)
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
            public TestHookB()
            {
                Priority = 2;
            }
        }

        class TestHookC : ConversationHookBase
        {
            public TestHookC()
            {
                Priority = 3;
            }
        }
    }
}