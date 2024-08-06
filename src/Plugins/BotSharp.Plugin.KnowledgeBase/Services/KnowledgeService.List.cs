namespace BotSharp.Plugin.KnowledgeBase.Services;

public partial class KnowledgeService
{
    public async Task<KnowledgeCollectionInfo> GetKnowledgeCollectionInfo(string collectionName)
    {
        try
        {
            var db = GetVectorDb();
            return await db.GetCollectionInfo(collectionName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when getting knowledge collectio info. {ex.Message}\r\n{ex.InnerException}");
            return new KnowledgeCollectionInfo();
        }
    }

    public async Task<UuidPagedItems<KnowledgeCollectionData>> GetKnowledgeCollectionData(KnowledgeFilter filter)
    {
        try
        {
            var db = GetVectorDb();
            return await db.GetCollectionData(filter);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when getting knowledge collectio data. {ex.Message}\r\n{ex.InnerException}");
            return new UuidPagedItems<KnowledgeCollectionData>();
        }
    }
}
