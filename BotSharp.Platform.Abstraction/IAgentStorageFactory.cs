using BotSharp.Platform.Abstraction;
using BotSharp.Platform.Models;
using System.Threading.Tasks;

namespace BotSharp.Platform.Abstraction
{
    public interface IAgentStorageFactory<TAgent> where TAgent : AgentBase
    {
        Task<IAgentStorage<TAgent>> Get();
    }
}
