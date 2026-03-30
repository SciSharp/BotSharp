using BotSharp.Abstraction.Hooks;

namespace BotSharp.Abstraction.MLTasks;

public interface IText2SqlHook : IHookBase
{
    // Get database type
    string GetDatabaseType(RoleDialogModel message);
    string? GetConnectionString(RoleDialogModel message, string? dataSource = null);
    Task SqlGenerated(RoleDialogModel message);
    Task SqlExecuting(RoleDialogModel message);
    Task SqlExecuted(RoleDialogModel message);
}
