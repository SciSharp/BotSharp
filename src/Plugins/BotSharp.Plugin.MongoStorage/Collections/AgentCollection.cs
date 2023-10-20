using BotSharp.Plugin.MongoStorage.Models;

namespace BotSharp.Plugin.MongoStorage.Collections;

public class AgentCollection : MongoBase
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Instruction { get; set; }
    public List<AgentTemplateMongoElement> Templates { get; set; }
    public List<FunctionDefMongoElement> Functions { get; set; }
    public List<AgentResponseMongoElement> Responses { get; set; }
    public List<string> Samples { get; set; }
    public bool IsPublic { get; set; }
    public bool AllowRouting { get; set; }
    public bool Disabled { get; set; }
    public List<string> Profiles { get; set; }
    public List<RoutingRuleMongoElement> RoutingRules { get; set; }

    public DateTime CreatedTime { get; set; }
    public DateTime UpdatedTime { get; set; }
}