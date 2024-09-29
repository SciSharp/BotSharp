using Microsoft.Data.Sqlite;

namespace BotSharp.Plugin.ExcelHandler.Helpers;

public interface IDbHelpers
{
    SqliteConnection GetPhysicalDbConnection();
    SqliteConnection GetInMemoryDbConnection();
}
