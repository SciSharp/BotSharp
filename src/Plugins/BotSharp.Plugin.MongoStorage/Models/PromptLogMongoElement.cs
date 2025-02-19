namespace BotSharp.Plugin.MongoStorage.Models;

[BsonIgnoreExtraElements(Inherited = true)]
public class PromptLogMongoElement
{
    public string MessageId { get; set; } = default!;
    public string AgentId { get; set; } = default!;
    public string Prompt { get; set; } = default!;
    public string? Response { get; set; }
    public DateTime CreateDateTime { get; set; }
}
