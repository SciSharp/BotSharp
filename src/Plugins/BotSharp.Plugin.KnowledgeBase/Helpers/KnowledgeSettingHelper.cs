namespace BotSharp.Plugin.KnowledgeBase.Helpers;

public static class KnowledgeSettingHelper
{
    public static ITextEmbedding GetTextEmbeddingSetting(IServiceProvider services, string collectionName)
    {
        var db = services.GetRequiredService<IBotSharpRepository>();
        var config = db.GetKnowledgeCollectionConfig(collectionName);
        var found = config?.TextEmbedding;
        var provider = found?.Provider;
        var model = found?.Model;
        var dimension = found?.Dimension ?? 0;

        if (found == null)
        {
            var settings = services.GetRequiredService<KnowledgeBaseSettings>();
            provider = settings.Default.TextEmbedding.Provider;
            model = settings.Default.TextEmbedding.Model;
            dimension = settings.Default.TextEmbedding.Dimension;
        }

        var embedding = services.GetServices<ITextEmbedding>().FirstOrDefault(x => x.Provider == provider);

        if (dimension <= 0)
        {
            dimension = GetLlmTextEmbeddingDimension(services, provider, model);
        }

        embedding.SetModelName(model);
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
