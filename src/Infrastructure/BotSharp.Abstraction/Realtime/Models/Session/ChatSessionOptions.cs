using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BotSharp.Abstraction.Realtime.Models.Session;

public class ChatSessionOptions
{
    public string Provider { get; set; }
    public int? BufferSize { get; set; }
    public JsonSerializerOptions? JsonOptions { get; set; }
    public ILogger? Logger { get; set; }
}
