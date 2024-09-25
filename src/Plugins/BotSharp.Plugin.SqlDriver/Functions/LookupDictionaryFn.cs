using Azure;
using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Core.Infrastructures;
using BotSharp.Plugin.SqlDriver.Models;
using MySqlConnector;
using static Dapper.SqlMapper;

namespace BotSharp.Plugin.SqlDriver.Functions;

public class LookupDictionaryFn : IFunctionCallback
{
    public string Name => "sql_dictionary_lookup";
    private readonly IServiceProvider _services;

    public LookupDictionaryFn(IServiceProvider services)
    {
        _services = services;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<LookupDictionary>(message.FunctionArgs);

        // check if need to instantely
        var settings = _services.GetRequiredService<SqlDriverSetting>();
        using var connection = new MySqlConnection(settings.MySqlExecutionConnectionString);
        var result = connection.Query(args.SqlStatement);

        if (result == null)
        {
            message.Content = "Record not found";
        }
        else
        {
            message.Content = JsonSerializer.Serialize(result);
        }
        var states = _services.GetRequiredService<IConversationStateService>();
        var dictionaryItems = states.GetState("dictionary_items", "");
        dictionaryItems += "\r\n\r\n" +  args.Reason + ":\r\n" + message.Content + "\r\n";
        states.SetState("dictionary_items", dictionaryItems);

        return true;
    }
}
