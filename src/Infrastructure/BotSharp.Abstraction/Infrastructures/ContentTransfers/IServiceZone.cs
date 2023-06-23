using BotSharp.Abstraction.Infrastructures.ContentTransmitters;

namespace BotSharp.Abstraction.Infrastructures.ContentTransfers;

public interface IServiceZone
{
    int Priority { get; }
    Task Serving(ContentContainer content);
}
