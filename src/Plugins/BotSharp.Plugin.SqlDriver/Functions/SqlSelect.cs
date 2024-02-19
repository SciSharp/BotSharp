using BotSharp.Plugin.SqlDriver.Models;
using MySqlConnector;
using static Dapper.SqlMapper;

namespace BotSharp.Plugin.SqlDriver.Functions;

public class SqlSelect : IFunctionCallback
{
    public string Name => "sql_select";
    private readonly IServiceProvider _services;

    public SqlSelect(IServiceProvider services)
    {
        _services = services;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<SqlStatement>(message.FunctionArgs);
        // check if need to instantely
        var execNow = !args.Parameters.Any(x => x.Value.StartsWith("@"));
        if (execNow)
        {
            var settings = _services.GetRequiredService<SqlDriverSetting>();
            using var connection = new MySqlConnection(settings.MySqlConnectionString);
            var dictionary = new Dictionary<string, object>();
            foreach(var p in args.Parameters)
            {
                dictionary["@" + p.Name] = p.Value;
            }
            var result = connection.QueryFirst<string>(args.Statement, dictionary);

            if (args.IsCheckExistence)
            {
                message.Content = result == null ?
                    $"The record does not exist" :
                    $"The record already exists";
            }
            else
            {
                message.Content = $"Retrieved result is {result} ({args.Reason})";
            }
        }
        else
        {
            var sqlDriver = _services.GetRequiredService<SqlDriverService>();
            sqlDriver.Enqueue(args);
            message.Content = $"Success.";
        }
        
        return true;
    }
}
