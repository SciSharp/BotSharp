namespace BotSharp.Abstraction.Routing.Executor;

public interface IFunctionExecutor
{
    public Task<bool> ExecuteAsync(RoleDialogModel message);
    public Task<string> GetIndicatorAsync(RoleDialogModel message);
}
