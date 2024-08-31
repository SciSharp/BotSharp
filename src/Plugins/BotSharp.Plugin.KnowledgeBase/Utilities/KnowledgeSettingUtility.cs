namespace BotSharp.Plugin.KnowledgeBase.Utilities;

public static class KnowledgeSettingUtility
{
    public static ITextEmbedding GetTextEmbeddingSetting(IServiceProvider services, string collectionName)
    {
        var settings = services.GetRequiredService<KnowledgeBaseSettings>();
        var found = settings.Collections.FirstOrDefault(x => x.Name == collectionName)?.TextEmbedding;
        if (found == null)
        {
            found = settings.Default.TextEmbedding;
        }

        var embedding = services.GetServices<ITextEmbedding>().FirstOrDefault(x => x.Provider == found.Provider);
        embedding.SetModelName(found.Model);
        embedding.SetDimension(found.Dimension);
        return embedding;
    }
}
