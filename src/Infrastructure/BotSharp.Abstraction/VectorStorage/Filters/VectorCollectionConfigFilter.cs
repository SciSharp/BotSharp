namespace BotSharp.Abstraction.VectorStorage.Filters;

public class VectorCollectionConfigFilter
{
    public IEnumerable<string>? CollectionNames { get; set; }
    public IEnumerable<string>? CollectionTypes { get; set; }
    public IEnumerable<string>? VectorStorageProviders { get; set; }

    public static VectorCollectionConfigFilter Empty()
    {
        return new VectorCollectionConfigFilter();
    }
}
