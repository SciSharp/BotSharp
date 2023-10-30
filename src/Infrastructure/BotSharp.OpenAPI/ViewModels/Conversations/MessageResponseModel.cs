using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Models;
using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Conversations;

public class MessageResponseModel : ITrackableMessage
{
    public string MessageId { get; set; }
    public string Text { get; set; }
    public string Function { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object Data { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FunctionCallFromLlm Instruction { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? RichContent { get; set; }
}
