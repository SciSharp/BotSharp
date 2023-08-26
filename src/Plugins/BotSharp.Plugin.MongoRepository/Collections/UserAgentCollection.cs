namespace BotSharp.Plugin.MongoRepository.Collections
{
    public class UserAgentCollection : MongoBase
    {
        public string UserId { get; set; }
        public string AgentId { get; set; }

        public DateTime CreatedTime { get; set; }
        public DateTime UpdatedTime { get; set; }
    }
}
