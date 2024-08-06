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
}
