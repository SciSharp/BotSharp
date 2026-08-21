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
            // Empty rather than an id: ConversationHookBase.IsMatch is
            // IsNullOrEmpty(SelfId) || SelfId == agentId, and this test resolves hooks with
            // GetHooksOrderByPriority(string.Empty) and asserts all three come back. Any non-empty
            // value here would match nothing and the count assertion would fail.
            public override string SelfId => string.Empty;

            public TestHookA()
            {
                Priority = 1;
            }
        }

        class TestHookB : ConversationHookBase
        {
            public override string SelfId => string.Empty;

            public TestHookB()
            {
                Priority = 2;
            }
        }

        class TestHookC : ConversationHookBase
        {
            public override string SelfId => string.Empty;

            public TestHookC()
            {
                Priority = 3;
            }
        }
    }
}