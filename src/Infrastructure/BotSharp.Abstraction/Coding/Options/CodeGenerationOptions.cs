namespace BotSharp.Abstraction.Coding.Options;

public class CodeGenerationOptions : LlmConfigBase
{
    /// <summary>
    /// Agent id
    /// </summary>
    public string? AgentId { get; set; }

    /// <summary>
    /// Template (prompt) name
    /// </summary>
    public string? TemplateName { get; set; }

    /// <summary>
    /// User description to generate code script
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The programming language
    /// </summary>
    public string? Language { get; set; } = "python";

    /// <summary>
    /// Code script name (e.g., demo.py)
    /// </summary>
    public string? CodeScriptName { get; set; }

    /// <summary>
    /// Code script type (i.e., src, test)
    /// </summary>
    public string? CodeScriptType { get; set; } = AgentCodeScriptType.Src;

    /// <summary>
    /// Data that can be used to fill in the prompt
    /// </summary>
    public Dictionary<string, object>? Data { get; set; }
}