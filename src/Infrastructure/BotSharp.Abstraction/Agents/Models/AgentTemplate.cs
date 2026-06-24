namespace BotSharp.Abstraction.Agents.Models;

public class AgentTemplate : AgentTemplateConfig
{
    public string Content { get; set; } = string.Empty;

    public AgentTemplate()
    {
    }

    public AgentTemplate(string name, string content)
    {
        Name = name;
        Content = content;
    }

    public override string ToString()
    {
        return Name;
    }
}

public class AgentTemplateConfig
{
    public string Name { get; set; }

    [JsonPropertyName("llm_config")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AgentTemplateLlmConfig? LlmConfig { get; set; }
}

public class AgentTemplateLlmConfig : LlmConfigBase
{
}