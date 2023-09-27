namespace BotSharp.Abstraction.Functions;

public interface IFunctionCallback
{
    string Name { get; }
    Task<bool> Execute(RoleDialogModel message);
}
