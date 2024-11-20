namespace BotSharp.Plugin.SqlDriver.Helpers;

internal static class SqlDriverHelper
{
    internal static string GetDatabaseType(IServiceProvider services)
    {
        var settings = services.GetRequiredService<SqlDriverSetting>();
        var dbType = "MySQL";

        if (!string.IsNullOrWhiteSpace(settings?.SqlServerConnectionString))
        {
            dbType = "SQL Server";
        }
        else if (!string.IsNullOrWhiteSpace(settings?.SqlLiteConnectionString))
        {
            dbType = "SQL Lite";
        }
        return dbType;
    }
}
