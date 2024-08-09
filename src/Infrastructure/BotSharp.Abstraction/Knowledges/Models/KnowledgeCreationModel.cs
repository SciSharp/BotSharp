using BotSharp.Abstraction.Knowledges.Enums;

namespace BotSharp.Abstraction.Knowledges.Models;

public class KnowledgeCreationModel
{
    public string Collection { get; set; } = KnowledgeCollectionName.BotSharp;
    public string Content { get; set; } = string.Empty;
}
