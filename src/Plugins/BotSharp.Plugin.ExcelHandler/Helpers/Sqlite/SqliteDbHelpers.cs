using BotSharp.Plugin.SqlDriver.Settings;
using Microsoft.Data.Sqlite;

namespace BotSharp.Plugin.ExcelHandler.Helpers.Sqlite;

public class SqliteDbHelpers : ISqliteDbHelpers
{
    private string _dbFilePath = string.Empty;
    private SqliteConnection inMemoryDbConnection = null;

    private readonly IServiceProvider _services;

    public SqliteDbHelpers(IServiceProvider service)
    {
        _services = service;
    }

    public SqliteConnection GetInMemoryDbConnection()
    {
        if (inMemoryDbConnection == null)
        {
            inMemoryDbConnection = new SqliteConnection("Data Source=:memory:;Mode=ReadWrite");
            inMemoryDbConnection.Open();
            return inMemoryDbConnection;
        }
        return inMemoryDbConnection;
    }

    public SqliteConnection GetPhysicalDbConnection()
    {
        if (string.IsNullOrEmpty(_dbFilePath))
        {
            var settingService = _services.GetRequiredService<SqlDriverSetting>();
            _dbFilePath = settingService.SqlLiteConnectionString;
        }

        var dbConnection = new SqliteConnection($"Data Source={_dbFilePath};Mode=ReadWrite");
        dbConnection.Open();
        return dbConnection;
    }
}
