namespace BotSharp.Abstraction.Repositories;

public interface IDatabaseHook
{
    // Get database type
    string GetDatabaseType(RoleDialogModel message);
}
