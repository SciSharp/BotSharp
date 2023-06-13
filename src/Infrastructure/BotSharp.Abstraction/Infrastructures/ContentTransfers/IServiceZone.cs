using BotSharp.Abstraction.Infrastructures.ContentTransmitters;

namespace BotSharp.Abstraction.Infrastructures.ContentTransfers;

public interface IServiceZone
{
    Task Serving(ContentContainer content);
}
