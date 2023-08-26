namespace BotSharp.Plugin.MongoRepository.Collections
{
    public class AgentCollection : MongoBase
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<string> Functions { get; set; }
        public string Instruction { get; set; }
        public List<string> Routes { get; set; }

        public DateTime CreatedTime { get; set; }
        public DateTime UpdatedTime { get; set; }
    }
}
