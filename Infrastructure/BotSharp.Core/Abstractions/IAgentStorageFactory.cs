using BotSharp.Platform.Models;
using System.Threading.Tasks;

namespace BotSharp.Platform.Abstractions
{
    public interface IAgentStorageFactory<TAgent> where TAgent : AgentBase
    {
        IAgentStorage<TAgent> Get();
    }
}
