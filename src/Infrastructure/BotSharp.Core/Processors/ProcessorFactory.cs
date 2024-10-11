using BotSharp.Abstraction.Processors;
using BotSharp.Abstraction.Processors.Models;

namespace BotSharp.Core.Processors;

public class ProcessorFactory
{
    private readonly IServiceProvider _services;

    public ProcessorFactory(IServiceProvider services)
    {
        _services = services;
    }

    public IEnumerable<IBaseProcessor<TInput, TOutput>> Create<TInput, TOutput>(string provider)
        where TInput : LlmBaseRequest where TOutput : class
    {
        var processors = _services.GetServices<IBaseProcessor<TInput, TOutput>>();
        processors = processors.Where(x => x.Provider == provider);
        return processors.OrderBy(x => x.Order);
    }

    public IBaseProcessor<TInput, TOutput>? Create<TInput, TOutput>(string provider, string name)
        where TInput : LlmBaseRequest where TOutput : class
    {
        var processors = _services.GetServices<IBaseProcessor<TInput, TOutput>>();
        return processors.FirstOrDefault(x => x.Provider == provider && x.Name == name);
    }
}
