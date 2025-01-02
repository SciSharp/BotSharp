using BotSharp.Abstraction.Instructs;

namespace BotSharp.Core.Instructs;

public partial class InstructService : IInstructService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<InstructService> _logger;

    public InstructService(
        IServiceProvider services,
        ILogger<InstructService> logger)
    {
        _services = services;
        _logger = logger;
    }
}
