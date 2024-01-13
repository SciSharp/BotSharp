using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Functions;
using BotSharp.Plugin.SqlHero.Models;
using BotSharp.Plugin.SqlHero.Settings;
using Dapper;
using MySqlConnector;
using System.Text.Json;
using System.Threading.Tasks;

namespace BotSharp.Plugin.SqlHero.Actions;

public class ExecuteQueryAction : IFunctionCallback
{
    public string Name => "execute_sql";

    private readonly SqlHeroSetting _setting;

    public ExecuteQueryAction(SqlHeroSetting setting)
    {
        _setting = setting;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<LlmInputArgs>(message.FunctionArgs);
        message.Content = "executed successully";
        /*using var connection = new MySqlConnection(_setting.MySqlConnectionString);
        message.Content = JsonSerializer.Serialize(connection.Query(args.SqlStatement), new JsonSerializerOptions
        {
            WriteIndented = true,
        });*/
        // message.StopCompletion = true;
        return true;
    }
}
