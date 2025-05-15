using BotSharp.Abstraction.Hooks;

namespace BotSharp.Plugin.SqlDriver.Interfaces;

public interface ISqlDriverHook : IHookBase
{
    // Get database type
    string GetDatabaseType(RoleDialogModel message);
    Task SqlGenerated(RoleDialogModel message);
    Task SqlExecuting(RoleDialogModel message);
    Task SqlExecuted(RoleDialogModel message);
}
