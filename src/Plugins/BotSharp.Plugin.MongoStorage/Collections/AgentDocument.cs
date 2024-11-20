namespace BotSharp.Plugin.MongoStorage.Collections;

public class AgentDocument : MongoBase
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Type { get; set; }
    public string? InheritAgentId { get; set; }
    public string? IconUrl { get; set; }
    public string Instruction { get; set; }
    public List<ChannelInstructionMongoElement> ChannelInstructions { get; set; }
    public List<AgentTemplateMongoElement> Templates { get; set; }
    public List<FunctionDefMongoElement> Functions { get; set; }
    public List<AgentResponseMongoElement> Responses { get; set; }
    public List<string> Samples { get; set; }
    public List<AgentUtilityMongoElement> Utilities { get; set; }
    public bool IsPublic { get; set; }
    public bool Disabled { get; set; }
    public List<string> Profiles { get; set; }
    public List<RoutingRuleMongoElement> RoutingRules { get; set; }
    public AgentLlmConfigMongoElement? LlmConfig { get; set; }

    public DateTime CreatedTime { get; set; }
    public DateTime UpdatedTime { get; set; }
}