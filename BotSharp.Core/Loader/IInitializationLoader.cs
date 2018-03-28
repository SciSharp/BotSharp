using System.Text;

namespace BotSharp.Core.Loader
{
    /// <summary>
    /// Implement a customzied loader
    /// </summary>
    public interface IInitializationLoader
    {
        int Priority { get; }
        void Initialize();
    }
}
