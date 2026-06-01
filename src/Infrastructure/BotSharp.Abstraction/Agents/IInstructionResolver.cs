namespace BotSharp.Abstraction.Agents;

public interface IInstructionResolver
{
    string Provider { get; }

    Task<string> ResolveAsync(Agent agent, string instruction, IEnumerable<object?> args, IDictionary<string, object?> kwArgs)
        => Task.FromResult(instruction);
}
