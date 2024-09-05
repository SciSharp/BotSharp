namespace BotSharp.Plugin.KnowledgeBase.Helpers;

public static class KnowledgeSettingHelper
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
        var dimension = found.Dimension;

        if (found.Dimension <= 0)
        {
            dimension = GetLlmTextEmbeddingDimension(services, found.Provider, found.Model);
        }

        embedding.SetModelName(found.Model);
        embedding.SetDimension(dimension);
        return embedding;
    }

    private static int GetLlmTextEmbeddingDimension(IServiceProvider services, string provider, string model)
    {
        var settings = services.GetRequiredService<ILlmProviderService>();
        var found = settings.GetSetting(provider, model);
        return found?.Dimension ?? 0;
    }
}
