using BotSharp.Abstraction.VectorStorage.Models;

namespace BotSharp.OpenAPI.ViewModels.Knowledges.Request;

public class CreateVectorCollectionIndexRequest
{
    public IEnumerable<CreateVectorCollectionIndexOptions> Options { get; set; } = [];
}

public class DeleteVectorCollectionIndexRequest
{
    public IEnumerable<DeleteVectorCollectionIndexOptions> Options { get; set; } = [];
}