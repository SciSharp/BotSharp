namespace BotSharp.Core.Loggers.Services;

public partial class LoggerService : ILoggerService
{
    private readonly IServiceProvider _services;
    private readonly IUserIdentity _user;
    private readonly ILogger<LoggerService> _logger;

    public LoggerService(
        IServiceProvider services,
        IUserIdentity user,
        ILogger<LoggerService> logger)
    {
        _services = services;
        _user = user;
        _logger = logger;
    }
}
