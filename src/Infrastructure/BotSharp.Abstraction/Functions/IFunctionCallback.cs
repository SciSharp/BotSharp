namespace BotSharp.Abstraction.Functions;

public interface IFunctionCallback
{
    string Name { get; }

    /// <summary>
    /// Indicator message used to provide UI feedback for function execution
    /// </summary>
    string Indication => string.Empty;

    Task<string> GetIndication(RoleDialogModel message) => Task.FromResult(Indication);

    Task<bool> Execute(RoleDialogModel message);
}
