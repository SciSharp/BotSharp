using System.Text.Json.Serialization;

namespace BotSharp.Core.Rules.Criteria.Code;

/// <summary>
/// Settings for <see cref="CodeCriteriaEvaluator"/>, parsed from
/// <c>CriteriaOptions.Data</c>.
/// </summary>
public class CodeCriteriaSettings
{
    /// <summary>
    /// Code processor provider (defaults to the Python interpreter).
    /// </summary>
    [JsonPropertyName("code_processor")]
    public string? CodeProcessor { get; set; }

    /// <summary>
    /// Code script name.
    /// </summary>
    [JsonPropertyName("code_script_name")]
    public string? CodeScriptName { get; set; }

    /// <summary>
    /// Argument name as an input key to the code script.
    /// </summary>
    [JsonPropertyName("argument_name")]
    public string? ArgumentName { get; set; }

    /// <summary>
    /// Json arguments as an input value to the code script.
    /// </summary>
    [JsonPropertyName("argument_content")]
    public JsonDocument? ArgumentContent { get; set; }
}
