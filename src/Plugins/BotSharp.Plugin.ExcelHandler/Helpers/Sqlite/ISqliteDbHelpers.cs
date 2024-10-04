using Microsoft.Data.Sqlite;

namespace BotSharp.Plugin.ExcelHandler.Helpers.Sqlite;

public interface ISqliteDbHelpers
{
    SqliteConnection GetPhysicalDbConnection();
    SqliteConnection GetInMemoryDbConnection();
}
