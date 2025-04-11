using BotSharp.Abstraction.Realtime.Enums;
using BotSharp.Abstraction.Realtime.Models;
using System.Threading;

namespace BotSharp.Abstraction.Realtime;

public interface IStreamChannel
{
    Task ConnectAsync(string conversationId);
    Task<StreamReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellation);
    Task SendAsync(byte[] data, CancellationToken cancellation);
    void ClearBuffer();
    Task CloseAsync(StreamChannelStatus status, string description, CancellationToken cancellation);
}
