using BotSharp.Platform.Abstraction;
using BotSharp.Platform.Models;
using System.Threading.Tasks;

namespace BotSharp.Core
{
    public interface IAgentStorageFactory
    {
        Task<IAgentStorage<TAgent>> Get<TAgent>() where TAgent : AgentBase;
    }
}
