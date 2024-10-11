using BotSharp.Abstraction.Processors.Models;

namespace BotSharp.Abstraction.Processors;

public interface IBaseProcessor<TInput, TOutput> where TInput : LlmBaseRequest where TOutput : class
{
    string Provider { get; }
    string Name => string.Empty;
    int Order { get; }

    Task<TOutput> Execute(TInput input);
}
