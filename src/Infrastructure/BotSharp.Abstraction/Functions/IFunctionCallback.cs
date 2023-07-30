namespace BotSharp.Abstraction.Functions;

public interface IFunctionCallback
{
    string Name { get; }
    Task<string> Execute(string args);
}
