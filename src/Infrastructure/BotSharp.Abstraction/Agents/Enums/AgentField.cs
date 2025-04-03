namespace BotSharp.Abstraction.Agents.Enums;

public enum AgentField
{
    All = 1,
    Name,
    Description,
    IsPublic,
    Disabled,
    Type,
    InheritAgentId,
    Profile,
    Label,
    RoutingRule,
    Instruction,
    Function,
    Template,
    Response,
    Sample,
    LlmConfig,
    Utility,
    McpTool,
    KnowledgeBase,
    Rule,
    MaxMessageCount
}

public enum AgentTaskField
{
    All = 1,
    Name,
    Description,
    Enabled,
    Content,
    Status
}
