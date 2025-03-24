using BotSharp.Abstraction.VectorStorage.Models;

namespace BotSharp.Abstraction.VectorStorage;

public interface IVectorDb
{
    string Provider { get; }

    Task<bool> DoesCollectionExist(string collectionName)
        => throw new NotImplementedException();
    Task<IEnumerable<string>> GetCollections()
        => throw new NotImplementedException();
    Task<StringIdPagedItems<VectorCollectionData>> GetPagedCollectionData(string collectionName, VectorFilter filter)
        => throw new NotImplementedException();
    Task<IEnumerable<VectorCollectionData>> GetCollectionData(string collectionName, IEnumerable<Guid> ids,
        bool withPayload = false, bool withVector = false)
        => throw new NotImplementedException();
    Task<bool> CreateCollection(string collectionName, int dimension)
        => throw new NotImplementedException();
    Task<bool> DeleteCollection(string collectionName)
        => throw new NotImplementedException();
    Task<bool> Upsert(string collectionName, Guid id, float[] vector, string text, Dictionary<string, object>? payload = null)
        => throw new NotImplementedException();
    Task<IEnumerable<VectorCollectionData>> Search(string collectionName, float[] vector, IEnumerable<string>? fields,
        int limit = 5, float confidence = 0.5f, bool withVector = false)
        => throw new NotImplementedException();
    Task<bool> DeleteCollectionData(string collectionName, List<Guid> ids)
        => throw new NotImplementedException();
    Task<bool> DeleteCollectionAllData(string collectionName)
        => throw new NotImplementedException();
    Task<IEnumerable<VectorCollectionSnapshot>> GetCollectionSnapshots(string collectionName)
        => throw new NotImplementedException();
    Task<VectorCollectionSnapshot?> CreateCollectionShapshot(string collectionName)
        => throw new NotImplementedException();
    Task<BinaryData> DownloadCollectionSnapshot(string collectionName, string snapshotFileName)
        => throw new NotImplementedException();
    Task<bool> RecoverCollectionFromShapshot(string collectionName, string snapshotFileName, BinaryData snapshotData)
        => throw new NotImplementedException();
    Task<bool> DeleteCollectionShapshot(string collectionName, string snapshotName)
        => throw new NotImplementedException();
}
