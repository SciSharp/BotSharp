using System.Threading.Tasks;

namespace BotSharp.Platform.Abstractions
{
    public interface IContextStorageFactory<T>
    {
        IContextStorage<T> Get();
    }
}
