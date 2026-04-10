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

    /// <summary>
    /// Response format: json, xml, markdown, yaml, etc.
    /// </summary>
    [JsonPropertyName("response_format")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ResponseFormat { get; set; }

    [JsonPropertyName("llm_config")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AgentTemplateLlmConfig? LlmConfig { get; set; }
}

public class AgentTemplateLlmConfig : LlmConfigBase
{

}