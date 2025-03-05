namespace BotSharp.Plugin.MongoStorage.Collections;

public class ConversationDocument : MongoBase
{
    public string AgentId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public string? TaskId { get; set; }
    public string Title { get; set; } = default!;
    public string TitleAlias { get; set; } = default!;
    public string Channel { get; set; } = default!;
    public string ChannelId { get; set; } = default!;
    public string Status { get; set; } = default!;
    public int DialogCount { get; set; }
    public List<string> Tags { get; set; } = [];
    public DateTime CreatedTime { get; set; }
    public DateTime UpdatedTime { get; set; }
    public Dictionary<string, BsonDocument> LatestStates { get; set; } = new();
}
