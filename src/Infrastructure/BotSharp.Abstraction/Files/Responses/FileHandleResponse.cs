namespace BotSharp.Abstraction.Files.Responses;

public class FileHandleResponse
{
    public string Result { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMsg { get; set; }
}
