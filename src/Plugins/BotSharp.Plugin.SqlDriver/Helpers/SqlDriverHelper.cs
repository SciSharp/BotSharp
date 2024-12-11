namespace BotSharp.Plugin.SqlDriver.Helpers;

internal static class SqlDriverHelper
{
    internal static string GetDatabaseType(IServiceProvider services)
    {
        var settings = services.GetRequiredService<SqlDriverSetting>();
        var dbType = "mysql";

        if (!string.IsNullOrWhiteSpace(settings?.SqlServerConnectionString))
        {
            dbType = "sqlserver";
        }
        else if (!string.IsNullOrWhiteSpace(settings?.SqlLiteConnectionString))
        {
            dbType = "sqllite";
        }
        return dbType;
    }
}
