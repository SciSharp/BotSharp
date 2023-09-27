using BotSharp.Abstraction.Routing.Settings;
using Microsoft.Extensions.Logging;

namespace BotSharp.Abstraction.Routing;

public abstract class RoutingHandlerBase
{
    protected Agent _router;
    protected readonly IServiceProvider _services;
    protected readonly ILogger _logger;
    protected RoutingSettings _settings;
    protected List<RoleDialogModel> _dialogs;

    public RoutingHandlerBase(IServiceProvider services,
        ILogger logger,
        RoutingSettings settings)
    {
        _services = services;
        _logger = logger;
        _settings = settings;
    }

    public void SetRouter(Agent router)
    {
        _router = router;
    }

    public void SetDialogs(List<RoleDialogModel> dialogs)
    {
        _dialogs = dialogs;
    }
}
