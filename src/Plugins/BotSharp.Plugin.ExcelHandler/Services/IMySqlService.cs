namespace BotSharp.Plugin.ExcelHandler.Services;

public interface IMySqlService : IDbService
{
    public bool DeleteTableSqlQuery();
}
