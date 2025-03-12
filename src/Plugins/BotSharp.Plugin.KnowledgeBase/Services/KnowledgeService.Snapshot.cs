namespace BotSharp.Plugin.KnowledgeBase.Services;

public partial class KnowledgeService
{
    public async Task<IEnumerable<VectorCollectionSnapshot>> GetVectorCollectionSnapshots(string collectionName)
    {
        if (string.IsNullOrWhiteSpace(collectionName))
        {
            return Enumerable.Empty<VectorCollectionSnapshot>();
        }

        var db = GetVectorDb();
        var snapshots = await db.GetCollectionSnapshots(collectionName);
        return snapshots;
    }

    public async Task<VectorCollectionSnapshot?> CreateVectorCollectionSnapshot(string collectionName)
    {
        if (string.IsNullOrWhiteSpace(collectionName))
        {
            return null;
        }

        var db = GetVectorDb();
        var snapshot = await db.CreateCollectionShapshot(collectionName);
        return snapshot;
    }

    public async Task<BinaryData> DownloadVectorCollectionSnapshot(string collectionName, string snapshotFileName)
    {
        if (string.IsNullOrWhiteSpace(collectionName) || string.IsNullOrWhiteSpace(snapshotFileName))
        {
            return BinaryData.Empty;
        }

        var db = GetVectorDb();
        var snapshot = await db.DownloadCollectionSnapshot(collectionName, snapshotFileName);
        return snapshot;
    }

    public async Task<bool> RecoverVectorCollectionFromSnapshot(string collectionName, string snapshotFileName, BinaryData snapshotData)
    {
        if (string.IsNullOrWhiteSpace(collectionName))
        {
            return false;
        }

        var db = GetVectorDb();
        var done = await db.RecoverCollectionFromShapshot(collectionName, snapshotFileName, snapshotData);
        return done;
    }

    public async Task<bool> DeleteVectorCollectionSnapshot(string collectionName, string snapshotName)
    {
        if (string.IsNullOrWhiteSpace(collectionName) || string.IsNullOrWhiteSpace(snapshotName))
        {
            return false;
        }

        var db = GetVectorDb();
        var done = await db.DeleteCollectionShapshot(collectionName, snapshotName);
        return done;
    }
}
