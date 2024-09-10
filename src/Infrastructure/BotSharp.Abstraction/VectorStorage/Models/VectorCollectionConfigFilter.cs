namespace BotSharp.Abstraction.VectorStorage.Models;

public class VectorCollectionConfigFilter
{
    public IEnumerable<string>? CollectionNames { get; set; }
    public IEnumerable<string>? CollectionTypes { get; set; }
    public IEnumerable<string>? VectorStroageProviders { get; set; }

    public static VectorCollectionConfigFilter Empty()
    {
        return new VectorCollectionConfigFilter();
    }
}
