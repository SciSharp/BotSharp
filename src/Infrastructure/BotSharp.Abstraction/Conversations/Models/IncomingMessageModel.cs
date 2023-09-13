using System.Text.Json.Serialization;

namespace BotSharp.Abstraction.Conversations.Models;

public class IncomingMessageModel
{
    public string Text { get; set; } = string.Empty;

    public virtual string Channel { get; set; } = string.Empty;

    /// <summary>
    /// Model name
    /// </summary>
    [JsonPropertyName("model")]
    public virtual string ModelName { get; set; } = "gpt-3.5-turbo";

    /// <summary>
    /// The sampling temperature to use that controls the apparent creativity of generated completions.
    /// </summary>
    public float Temperature { get; set; } = 0.5f;

    /// <summary>
    /// An alternative value to Temperature, called nucleus sampling, that causes
    /// the model to consider the results of the tokens with probability mass.
    /// </summary>
    public float SamplingFactor { get; set; } = 0.5f;

    /// <summary>
    /// Conversation states from input
    /// </summary>
    public List<string> States { get; set; } = new List<string>();
}
