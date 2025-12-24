using BotSharp.Abstraction.VectorStorage.Filters;

namespace BotSharp.Plugin.KnowledgeBase.Helpers;

public static class KnowledgeSettingHelper
{
    public static async Task<ITextEmbedding> GetTextEmbeddingSetting(IServiceProvider services, string collectionName)
    {
        var settings = services.GetRequiredService<KnowledgeBaseSettings>();
        var db = services.GetRequiredService<IBotSharpRepository>();

        // Get collection config from db
        var config = await db.GetKnowledgeCollectionConfig(collectionName, settings.VectorDb.Provider);

        var textEmbeddingConfig = config?.TextEmbedding;
        var provider = textEmbeddingConfig?.Provider ?? settings.Default.TextEmbedding.Provider;
        var model = textEmbeddingConfig?.Model ?? settings.Default.TextEmbedding.Model;
        var dimension = textEmbeddingConfig?.Dimension ?? settings.Default.TextEmbedding.Dimension;

        // Set up text embedding
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
        return found?.Embedding?.Dimension ?? 0;
    }
}
