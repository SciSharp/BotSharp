using BotSharp.Abstraction.Realtime.Enums;

namespace BotSharp.Abstraction.Realtime.Models;

public class StreamReceiveResult
{
    public StreamChannelStatus Status { get; set; }
    public int Count { get; set; }
    public bool EndOfMessage { get; }
}
