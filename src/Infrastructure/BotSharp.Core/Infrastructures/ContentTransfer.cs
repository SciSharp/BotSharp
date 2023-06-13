using BotSharp.Abstraction.Infrastructures.ContentTransfers;
using BotSharp.Abstraction.Infrastructures.ContentTransmitters;
using BotSharp.Abstraction.Users;

namespace BotSharp.Core.Infrastructures;

public class ContentTransfer : IContentTransfer
{
    private readonly IServiceProvider _services;
    private readonly ICurrentUser _user;

    public ContentTransfer(IServiceProvider services, ICurrentUser user)
    {
        _services = services;
        _user = user;
    }

    public async Task<TransportResult> Transport(ContentContainer input)
    {
        input.UserId = _user.Id;

        var result = new TransportResult
        {
            IsSuccess = true,
            Messages = new List<string>()
        };

        var zones = _services.GetServices<IServiceZone>();

        foreach (var zone in zones)
        {
            input.Output = null;

            try
            {
                await zone.Serving(input);
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Messages.Add(ex.Message);
            }
        }

        return result;
    }
}
