using BotSharp.Abstraction.VectorStorage.Models;

namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class VectorCollectionConfigViewModel : VectorCollectionConfig
{
    public static VectorCollectionConfigViewModel From(VectorCollectionConfig model)
    {
        return new VectorCollectionConfigViewModel
        {
            Name = model.Name,
            Type = model.Type,
            VectorStore = model.VectorStore,
            TextEmbedding = model.TextEmbedding
        };
    }
}
