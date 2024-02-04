namespace BotSharp.Plugin.MongoStorage.Collections;

public class AgentTaskDocument : MongoBase
{
    public string Name { get; set; }
    public string? Description { get; set; }
    public string Content { get; set; }
    public bool Enabled { get; set; }
    public string AgentId { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime UpdatedTime { get; set; }
}
