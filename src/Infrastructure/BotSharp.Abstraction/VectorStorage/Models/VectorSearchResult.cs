namespace BotSharp.Abstraction.VectorStorage.Models;

public class VectorSearchResult : VectorCollectionData
{
    public VectorSearchResult()
    {

    }

    public static VectorSearchResult CopyFrom(VectorCollectionData data)
    {
        return new VectorSearchResult
        {
            Id = data.Id,
            Data = data.Data,
            Score = data.Score,
            Vector = data.Vector
        };
    }
}