namespace BotSharp.Abstraction.Knowledges.Settings;

public class KnowledgeBaseSettings
{
    public string VectorDb { get; set; }
    public KnowledgeModelSetting TextEmbedding { get; set; }
    public string Pdf2TextConverter { get; set; }
}

public class KnowledgeModelSetting
{
    public string Provider { get; set; }
    public string Model { get; set; }
}