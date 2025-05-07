using System.Text.Json;

namespace BotSharp.Abstraction.Realtime.Models.Session;

public class ChatSessionOptions
{
    public int? BufferSize { get; set; }
    public JsonSerializerOptions? JsonOptions { get; set; }
}
