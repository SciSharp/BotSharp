using BotSharp.Abstraction.Instructs.Options;
using System.Threading.Tasks;

namespace BotSharp.Abstraction.Rules;

public interface IUniversalParsingEngine
{
    Task<T?> ParseAsync<T>(string text, InstructOptions? options = null) where T : class;
}
