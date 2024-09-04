namespace BotSharp.Core.Knowledges.Services;

public partial class KnowledgeService
{
    public async Task<bool> DeleteVectorCollection(string collectionName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(collectionName))
            {
                return false;
            }

            var db = GetVectorDb();
            return await db.DeleteCollection(collectionName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when deleting collection ({collectionName}). {ex.Message}\r\n{ex.InnerException}");
            return false;
        }
    }

    public async Task<bool> DeleteVectorCollectionData(string collectionName, string id)
    {
        try
        {
            if (!Guid.TryParse(id, out var guid))
            {
                return false;
            }

            var db = GetVectorDb();
            return await db.DeleteCollectionData(collectionName, guid);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when deleting vector collection data ({collectionName}-{id}). {ex.Message}\r\n{ex.InnerException}");
            return false;
        }
    }
}
