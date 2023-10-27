using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Models;

namespace BotSharp.Abstraction.Conversations.Models;

public class RoleDialogModel : ITrackableMessage
{
    public string MessageId { get; set; }

    /// <summary>
    /// user, system, assistant, function
    /// </summary>
    public string Role { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Content { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string CurrentAgentId { get; set; }

    /// <summary>
    /// Function name if LLM response function call
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FunctionName { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FunctionArgs { get; set; }

    /// <summary>
    /// Function execution structured data, this data won't pass to LLM.
    /// It's ideal to render in rich content in UI.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object Data { get; set; }

    /// <summary>
    /// Stop conversation completion
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public bool StopCompletion { get; set; }

    public FunctionCallFromLlm Instruction { get; set; }

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
}
