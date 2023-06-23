namespace BotSharp.Core.Infrastructures;

public class ContentTransfer : IContentTransfer
{
    private readonly IServiceProvider _services;

    public ContentTransfer(IServiceProvider services)
    {
        _services = services;
    }

    public async Task<TransportResult> Transport(ContentContainer input)
    {
        input.Output = new RoleDialogModel();

        var result = new TransportResult
        {
            IsSuccess = true,
            Messages = new List<string>()
        };

        var zones = _services.GetServices<IChatServiceZone>()
            .OrderBy(x => x.Priority)
            .ToList();

        foreach (var zone in zones)
        {
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
