using System.Text.Json;

namespace BotSharp.Abstraction.Rules;

public interface IRuleConfig
{
    string Provider { get; }

    Task<JsonDocument> GetConfigAsync();
}
