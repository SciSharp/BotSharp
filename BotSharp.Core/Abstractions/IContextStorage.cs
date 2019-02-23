using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Platform.Abstraction
{
    public interface IContextStorage<T>
    {
        Task<bool> Persist(string sessionId, T[] context);
        Task<T[]> Fetch(string sessionId);
    }
}
