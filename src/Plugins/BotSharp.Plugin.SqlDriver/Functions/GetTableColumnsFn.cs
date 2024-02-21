using BotSharp.Abstraction.Repositories;
using BotSharp.Plugin.SqlDriver.Models;
using System.IO;

namespace BotSharp.Plugin.SqlDriver.Functions;

public class GetTableColumnsFn : IFunctionCallback
{
    public string Name => "get_table_columns";
    private readonly IServiceProvider _services;

    public GetTableColumnsFn(IServiceProvider services)
    {
        _services = services;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<GetTableColumnsArgs>(message.FunctionArgs);
        message.Content = "";

        var dbSettings = _services.GetRequiredService<BotSharpDatabaseSettings>();
        var dir = Path.Combine(dbSettings.FileRepository, "agents", "beda4c12-e1ec-4b4b-b328-3df4a6687c4f", "schemas");

        // Search related document by message.Content + args.Description
        var files = Directory.GetFiles(dir);
        foreach (var file in files)
        {
            var fileName = file.Split(Path.DirectorySeparatorChar).Last();
            if (fileName.Split('.').First() == args.Table)
            {
                message.Content += File.ReadAllText(file);
                break;
            }
        }
        return true;
    }
}
