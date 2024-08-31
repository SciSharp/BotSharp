using BotSharp.Abstraction.Knowledges.Enums;

namespace BotSharp.Abstraction.Knowledges.Settings;

public class KnowledgeBaseSettings
{
    public string VectorDb { get; set; }
    public string GraphDb { get; set; }

    public DefaultKnowledgeBaseSetting Default { get; set; }
    public List<VectorCollectionSetting> Collections { get; set; } = new();
}

public class DefaultKnowledgeBaseSetting
{
    public string CollectionName { get; set; } = KnowledgeCollectionName.BotSharp;
    public KnowledgeTextEmbeddingSetting TextEmbedding { get; set; }
}

public class VectorCollectionSetting
{
    public string Name { get; set; }
    public KnowledgeTextEmbeddingSetting TextEmbedding { get; set; }
}

public class KnowledgeTextEmbeddingSetting
{
    public string Provider { get; set; }
    public string Model { get; set; }
    public int Dimension { get; set; }
}