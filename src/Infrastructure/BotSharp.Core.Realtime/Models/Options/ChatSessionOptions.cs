namespace BotSharp.Core.Realtime.Models.Options;

public class ChatSessionOptions
{
    public int? BufferSize { get; set; }
    public JsonSerializerOptions? JsonOptions { get; set; }
}
