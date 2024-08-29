namespace BotSharp.Plugin.KnowledgeBase.Services;

public partial class KnowledgeService
{
    public async Task FeedVectorKnowledge(string collectionName, KnowledgeCreationModel knowledge)
    {
        var index = 0;
        var lines = _textChopper.Chop(knowledge.Content, new ChunkOption
        {
            Size = 1024,
            Conjunction = 32,
            SplitByWord = true,
        });

        var db = GetVectorDb();
        var textEmbedding = GetTextEmbedding(collectionName);

        await db.CreateCollection(collectionName, textEmbedding.GetDimension());
        foreach (var line in lines)
        {
            var vec = await textEmbedding.GetVectorAsync(line);
            await db.Upsert(collectionName, Guid.NewGuid(), vec, line);
            index++;
            Console.WriteLine($"Saved vector {index}/{lines.Count}: {line}\n");
        }
    }

    public async Task<bool> CreateVectorCollection(string collectionName, int dimension)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(collectionName))
            {
                return false;
            }

            var db = GetVectorDb();
            return await db.CreateCollection(collectionName, dimension);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when creating a vector collection ({collectionName}). {ex.Message}\r\n{ex.InnerException}");
            return false;
        }
    }

    public async Task<bool> CreateVectorCollectionData(string collectionName, VectorCreateModel create)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(collectionName) || string.IsNullOrWhiteSpace(create.Text))
            {
                return false;
            }

            var textEmbedding = GetTextEmbedding(collectionName);
            var vector = await textEmbedding.GetVectorAsync(create.Text);

            var db = GetVectorDb();
            var guid = Guid.NewGuid();
            return await db.Upsert(collectionName, guid, vector, create.Text, create.Payload);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when creating vector collection data. {ex.Message}\r\n{ex.InnerException}");
            return false;
        }
    }
}
