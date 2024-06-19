namespace BotSharp.Abstraction.Models;

public class MessageConfig
{
    /// <summary>
    /// Completion Provider
    /// </summary>
    [JsonPropertyName("provider")]
    public virtual string? Provider { get; set; } = null;

    /// <summary>
    /// Model name
    /// </summary>
    [JsonPropertyName("model")]
    public virtual string? Model { get; set; } = null;

    /// <summary>
    /// Model name
    /// </summary>
    [JsonPropertyName("model_id")]
    public virtual string? ModelId { get; set; } = null;

    /// <summary>
    /// The sampling temperature to use that controls the apparent creativity of generated completions.
    /// </summary>
    public float Temperature { get; set; } = 0f;

    /// <summary>
    /// An alternative value to Temperature, called nucleus sampling, that causes
    /// the model to consider the results of the tokens with probability mass.
    /// </summary>
    public float SamplingFactor { get; set; } = 0f;

    /// <summary>
    /// Conversation states from input
    /// </summary>
    public List<MessageState> States { get; set; } = new List<MessageState>();

    /// <summary>
    /// Agent task id
    /// </summary>
    public string? TaskId { get; set; }
}
