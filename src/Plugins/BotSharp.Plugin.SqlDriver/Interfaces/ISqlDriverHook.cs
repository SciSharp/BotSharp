namespace BotSharp.Plugin.SqlDriver.Interfaces;

public interface ISqlDriverHook
{
    // Get database type
    string GetDatabaseType(RoleDialogModel message);
    Task SqlGenerated(RoleDialogModel message);
    Task SqlExecuting(RoleDialogModel message);
    Task SqlExecuted(RoleDialogModel message);
}
