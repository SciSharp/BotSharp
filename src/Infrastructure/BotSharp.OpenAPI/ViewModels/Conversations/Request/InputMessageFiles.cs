namespace BotSharp.OpenAPI.ViewModels.Conversations;

public class InputMessageFiles
{
    public List<MessageState> States { get; set; } = new();
    public List<FileDataModel> Files { get; set; } = new();
}
