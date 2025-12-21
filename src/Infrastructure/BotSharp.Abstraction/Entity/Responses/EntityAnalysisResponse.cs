using BotSharp.Abstraction.Entity.Models;

namespace BotSharp.Abstraction.Entity.Responses;

public class EntityAnalysisResponse : ResponseBase
{
    public List<EntityAnalysisResult> Results { get; set; } = [];
}
