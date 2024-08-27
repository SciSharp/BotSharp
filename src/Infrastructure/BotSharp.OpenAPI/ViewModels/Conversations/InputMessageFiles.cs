namespace BotSharp.OpenAPI.ViewModels.Conversations;

public class InputMessageFiles
{
    public List<MessageState> States { get; set; } = new();
    public List<BotSharpFile> Files { get; set; } = new();
}
