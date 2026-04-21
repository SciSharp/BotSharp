using BotSharp.Abstraction.VectorStorage.Models;

namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class KnowledgeCollectionConfigViewModel : VectorCollectionConfig
{
    public static KnowledgeCollectionConfigViewModel From(VectorCollectionConfig model)
    {
        return new KnowledgeCollectionConfigViewModel
        {
            Name = model.Name,
            Type = model.Type,
            VectorStore = model.VectorStore,
            TextEmbedding = model.TextEmbedding
        };
    }
}
