namespace BotSharp.Abstraction.Functions.Models;

/// <summary>
/// One tool call in a model's reply.
/// </summary>
/// <remarks>
/// A reply can carry several: models routinely ask for independent lookups at once, and every
/// provider here used to keep only the first. See <see cref="RoleDialogModel.ToolCalls"/> for
/// how the whole set is carried and how it relates to the single-call fields beside it.
/// </remarks>
public class LlmToolCall
{
    /// <summary>
    /// The provider's id for this call. It is what a tool result has to be sent back under, so
    /// results cannot be matched to calls without it.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// The name exactly as the model produced it, with no normalization applied -- a remote MCP
    /// tool may legitimately have a name that name repair would rewrite.
    /// </summary>
    public string? FunctionName { get; set; }

    /// <summary>
    /// Raw JSON arguments. The model does not always produce valid JSON, so parse defensively.
    /// </summary>
    public string? FunctionArgs { get; set; }

    public LlmToolCall()
    {
    }

    public LlmToolCall(string? id, string? functionName, string? functionArgs)
    {
        Id = id;
        FunctionName = functionName;
        FunctionArgs = functionArgs;
    }

    public override string ToString() => $"{FunctionName}({FunctionArgs}) [{Id}]";
}
