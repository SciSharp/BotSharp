using BotSharp.Abstraction.Knowledges.Enums;

namespace BotSharp.Abstraction.Knowledges.Settings;

public class KnowledgeBaseSettings
{
    public string DefaultCollection { get; set; } = KnowledgeCollectionName.BotSharp;
    public string VectorDb { get; set; }
    public string GraphDb { get; set; }
    public KnowledgeModelSetting TextEmbedding { get; set; }
}

public class KnowledgeModelSetting
{
    public string Provider { get; set; }
    public string Model { get; set; }
}