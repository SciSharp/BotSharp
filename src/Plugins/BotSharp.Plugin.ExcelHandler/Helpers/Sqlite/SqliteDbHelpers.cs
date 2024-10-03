using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using BotSharp.Plugin.SqlDriver.Models;
using BotSharp.Plugin.SqlHero.Settings;

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
