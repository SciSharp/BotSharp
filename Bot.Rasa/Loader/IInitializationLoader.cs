using System.Text;

namespace Bot.Rasa.Loader
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
