namespace BotSharp.Plugin.KnowledgeBase.Services;

public abstract partial class VectorOrchestratorBase
{
    #region Snapshot
    public virtual async Task<IEnumerable<KnowledgeCollectionSnapshot>> GetCollectionSnapshots(string collectionName, KnowledgeSnapshotOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(collectionName))
        {
            return [];
        }

        var vectorDb = GetVectorDb(options?.DbProvider);
        if (vectorDb == null)
        {
            return [];
        }

        var snapshots = await vectorDb.GetCollectionSnapshots(collectionName);
        return snapshots.Select(x => KnowledgeCollectionSnapshot.CopyFrom(x)!);
    }

    public virtual async Task<KnowledgeCollectionSnapshot?> CreateCollectionSnapshot(string collectionName, KnowledgeSnapshotOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(collectionName))
        {
            return null;
        }

        var vectorDb = GetVectorDb(options?.DbProvider);
        if (vectorDb == null)
        {
            return null;
        }

        var snapshot = await vectorDb.CreateCollectionShapshot(collectionName);
        return KnowledgeCollectionSnapshot.CopyFrom(snapshot);
    }

    public virtual async Task<BinaryData> DownloadCollectionSnapshot(string collectionName, string snapshotFileName, KnowledgeSnapshotOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(collectionName) || string.IsNullOrWhiteSpace(snapshotFileName))
        {
            return BinaryData.Empty;
        }

        var vectorDb = GetVectorDb(options?.DbProvider);
        if (vectorDb == null)
        {
            return BinaryData.Empty;
        }

        var snapshot = await vectorDb.DownloadCollectionSnapshot(collectionName, snapshotFileName);
        return snapshot;
    }

    public virtual async Task<bool> RecoverCollectionFromSnapshot(string collectionName, string snapshotFileName, BinaryData snapshotData, KnowledgeSnapshotOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(collectionName))
        {
            return false;
        }

        var vectorDb = GetVectorDb(options?.DbProvider);
        if (vectorDb == null)
        {
            return false;
        }

        var done = await vectorDb.RecoverCollectionFromShapshot(collectionName, snapshotFileName, snapshotData);
        return done;
    }

    public virtual async Task<bool> DeleteCollectionSnapshot(string collectionName, string snapshotName, KnowledgeSnapshotOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(collectionName) || string.IsNullOrWhiteSpace(snapshotName))
        {
            return false;
        }

        var vectorDb = GetVectorDb(options?.DbProvider);
        if (vectorDb == null)
        {
            return false;
        }

        var done = await vectorDb.DeleteCollectionShapshot(collectionName, snapshotName);
        return done;
    }
    #endregion
}
