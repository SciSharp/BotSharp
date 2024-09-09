namespace BotSharp.Plugin.KnowledgeBase.Services;

public partial class KnowledgeService
{
    public async Task FeedVectorKnowledge(string collectionName, KnowledgeCreationModel knowledge)
    {
        var index = 0;
        var lines = TextChopper.Chop(knowledge.Content, new ChunkOption
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
}
