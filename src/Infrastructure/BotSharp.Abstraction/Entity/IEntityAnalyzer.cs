using BotSharp.Abstraction.Entity.Models;
using BotSharp.Abstraction.Entity.Responses;

namespace BotSharp.Abstraction.Entity;

public interface IEntityAnalyzer
{
    string Provider { get; }

    Task<EntityAnalysisResponse> AnalyzeAsync(string text, EntityAnalysisOptions? options = null);
}
