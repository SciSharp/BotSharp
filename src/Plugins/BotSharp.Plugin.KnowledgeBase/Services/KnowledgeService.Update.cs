namespace BotSharp.Plugin.KnowledgeBase.Services;

public partial class KnowledgeService
{
    public async Task<bool> UpdateVectorCollectionData(string collectionName, VectorUpdateModel update)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(collectionName) || string.IsNullOrWhiteSpace(update.Text) || !Guid.TryParse(update.Id, out var guid))
            {
                return false;
            }

            var db = GetVectorDb();
            var found = await db.GetCollectionData(collectionName, new List<Guid> { guid });
            if (found.IsNullOrEmpty())
            {
                return false;
            }

            var textEmbedding = GetTextEmbedding(collectionName);
            var vector = await textEmbedding.GetVectorAsync(update.Text);
            return await db.Upsert(collectionName, guid, vector, update.Text, update.Payload);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when updating vector collection data. {ex.Message}\r\n{ex.InnerException}");
            return false;
        }
    }
}
