namespace BotSharp.Plugin.KnowledgeBase.Services;

public partial class KnowledgeService
{
    public async Task<bool> DeleteKnowledgeCollectionData(string collectionName, string id)
    {
        try
        {
            var db = GetVectorDb();
            return await db.DeleteCollectionData(collectionName, id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when deleting knowledge collection data ({collectionName}-{id}). {ex.Message}\r\n{ex.InnerException}");
            return false;
        }
    }
}
