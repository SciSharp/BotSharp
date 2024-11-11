using BotSharp.Abstraction.Instructs;
using BotSharp.Abstraction.Options;

namespace BotSharp.Core.Instructs;

public partial class InstructService : IInstructService
{
    private readonly IServiceProvider _services;
    private readonly BotSharpOptions _options;
    private readonly ILogger<InstructService> _logger;

    public InstructService(
        IServiceProvider services,
        BotSharpOptions options,
        ILogger<InstructService> logger)
    {
        _services = services;
        _options = options;
        _logger = logger;
    }
}
