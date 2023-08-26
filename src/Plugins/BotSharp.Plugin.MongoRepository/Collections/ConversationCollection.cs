namespace BotSharp.Plugin.MongoRepository.Collections;

public class ConversationCollection : MongoBase
{
    public string AgentId { get; set; }
    public string UserId { get; set; }
    public string Title { get; set; }
    public string Dialog { get; set; }
    public string State { get; set; }

    public DateTime CreatedTime { get; set; }
    public DateTime UpdatedTime { get; set; }
}
