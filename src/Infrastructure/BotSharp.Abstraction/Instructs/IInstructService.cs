namespace BotSharp.Abstraction.Instructs;

public interface IInstructService
{
    Task<bool> ExecuteInstructionRecursively(Agent agent,
        List<RoleDialogModel> wholeDialogs,
        Func<RoleDialogModel, Task> onMessageReceived,
        Func<RoleDialogModel, Task> onFunctionExecuting,
        Func<RoleDialogModel, Task> onFunctionExecuted);
}
