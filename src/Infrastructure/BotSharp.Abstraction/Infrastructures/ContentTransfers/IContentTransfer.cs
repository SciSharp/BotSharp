using BotSharp.Abstraction.Infrastructures.ContentTransfers;

namespace BotSharp.Abstraction.Infrastructures.ContentTransmitters;

public interface IContentTransfer
{
    Task<TransportResult> Transport(ContentContainer input);
}
