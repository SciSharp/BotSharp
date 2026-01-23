using BotSharp.Abstraction.Conversations.Models;

namespace BotSharp.Core.NRules.Services
{
    public class FactMapper
    {
        /// <summary>
        /// 将 Dictionary 转换为 POCO
        /// 示例：将 "user_level": "vip" 映射为 Customer 对象
        /// </summary>
        /// <param name="states"></param>
        /// <returns></returns>
        internal static IEnumerable<object> Map(IDictionary<string, string> states)
        {
            var facts = new List<object>();

            foreach (var state in states)
            {
                // Default mapping: Wrap every state into a StateKeyValue object
                // This allows rules to match against specific keys, e.g., Key == "user_level"
                facts.Add(new StateKeyValue(state.Key, new List<StateValue>
                {
                    new StateValue
                    {
                        Data = state.Value,
                        Active = true,
                        UpdateTime = DateTime.UtcNow
                    }
                }));
            }

            // TODO: Implement custom mapping logic to convert state groups into domain POCOs
            // Example:
            // if (states.TryGetValue("user_level", out var level))
            // {
            //     facts.Add(new Customer { Level = level });
            // }

            return facts;
        }
    }
}
