namespace BotSharp.Core.Rules.Services;

public partial class RuleAction : IRuleAction
{
    private readonly IServiceProvider _services;
    private readonly ILogger<RuleAction> _logger;

    public RuleAction(
        IServiceProvider services,
        ILogger<RuleAction> logger)
    {
        _services = services;
        _logger = logger;
    }

    public string Provider => RuleHandler.DefaultProvider;
}
