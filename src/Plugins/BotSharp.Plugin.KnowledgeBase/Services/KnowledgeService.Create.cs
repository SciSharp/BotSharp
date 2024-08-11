namespace BotSharp.Plugin.KnowledgeBase.Services;

public partial class KnowledgeService
{
    public async Task FeedKnowledge(string collectionName, KnowledgeCreationModel knowledge)
    {
        var index = 0;
        var lines = _textChopper.Chop(knowledge.Content, new ChunkOption
        {
            Size = 1024,
            Conjunction = 32,
            SplitByWord = true,
        });

        var db = GetVectorDb();
        var textEmbedding = GetTextEmbedding();

        await db.CreateCollection(collectionName, textEmbedding.Dimension);
        foreach (var line in lines)
        {
            var vec = await textEmbedding.GetVectorAsync(line);
            var id = Guid.NewGuid().ToString();
            await db.Upsert(collectionName, id, vec, line);
            index++;
            Console.WriteLine($"Saved vector {index}/{lines.Count}: {line}\n");
        }
    }
}
