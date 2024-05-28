using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Messaging;
using BotSharp.Abstraction.Messaging.Models.RichContent;

namespace BotSharp.Abstraction.Conversations.Models;

public class RoleDialogModel : ITrackableMessage
{
    /// <summary>
    /// If Role is Assistant, it is same as user's message id.
    /// </summary>
    public string MessageId { get; set; }

    /// <summary>
    /// user, system, assistant, function
    /// </summary>
    public string Role { get; set; }

    /// <summary>
    /// User id when Role is User
    /// </summary>
    public string SenderId { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string Content { get; set; }

    public string? SecondaryContent { get; set; }

    /// <summary>
    /// Postback content
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("payload")]
    public string? Payload { get; set; }

    /// <summary>
    /// Indicator message used to provide UI feedback for function execution
    /// </summary>
    public string? Indication { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string CurrentAgentId { get; set; }

    /// <summary>
    /// Function name if LLM response function call
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FunctionName { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ToolCallId { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PostbackFunctionName { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FunctionArgs { get; set; }

    /// <summary>
    /// Function execution structured data, this data won't pass to LLM.
    /// It's ideal to render in rich content in UI.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object Data { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string ImageUrl { get; set; }

    /// <summary>
    /// Remember to set Message.Content as well
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RichContent<IRichMessage>? RichContent { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RichContent<IRichMessage>? SecondaryRichContent { get; set; }

    /// <summary>
    /// Stop conversation completion
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public bool StopCompletion { get; set; }

    public FunctionCallFromLlm Instruction { get; set; }

    public List<BotSharpFile> Files { get; set; } = new List<BotSharpFile>();

    private RoleDialogModel()
    {
    }

    public RoleDialogModel(string role, string text)
    {
        Role = role;
        Content = text;
        MessageId = Guid.NewGuid().ToString();
    }

    public override string ToString()
    {
        if (Role == AgentRole.Function)
        {
            return $"{Role}: {FunctionName}({FunctionArgs}) => {Content}";
        }
        else
        {
            return $"{Role}: {Content}";
        }
    }

    public static RoleDialogModel From(RoleDialogModel source,
        string? role = null,
        string? content = null)
    {
        return new RoleDialogModel(role ?? source.Role, content ?? source.Content)
        {
            CurrentAgentId = source.CurrentAgentId,
            MessageId = source.MessageId,
            FunctionArgs = source.FunctionArgs,
            FunctionName = source.FunctionName,
            ToolCallId = source.ToolCallId,
            PostbackFunctionName = source.PostbackFunctionName,
            RichContent = source.RichContent,
            StopCompletion = source.StopCompletion,
            Instruction = source.Instruction,
            Data = source.Data
        };
    }
}
