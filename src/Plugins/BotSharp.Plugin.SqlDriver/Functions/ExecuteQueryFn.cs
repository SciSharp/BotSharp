namespace BotSharp.Plugin.SqlDriver.Functions;

public class ExecuteQueryFn : IFunctionCallback
{
    public string Name => "execute_sql";

    private readonly SqlDriverSetting _setting;

    public ExecuteQueryFn(SqlDriverSetting setting)
    {
        _setting = setting;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        message.Content = "Executed";
        /*using var connection = new MySqlConnection(_setting.MySqlConnectionString);
        message.Content = JsonSerializer.Serialize(connection.Query(args.SqlStatement), new JsonSerializerOptions
        {
            WriteIndented = true,
        });*/
        // message.StopCompletion = true;
        return true;
    }
}
