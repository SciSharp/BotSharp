namespace BotSharp.Abstraction.Conversations.Models;

public class PostbackMessageModel
{
    public string FunctionName { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    /// <summary>
    /// Parent message id
    /// </summary>
    public string ParentId { get; set; } = string.Empty;
}
