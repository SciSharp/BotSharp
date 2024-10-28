using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Functions;
using BotSharp.Plugin.CodeDriver.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace BotSharp.Plugin.CodeDriver.Functions;

public class SaveSourceCodeFn : IFunctionCallback
{
    public string Name => "save_source_code";

    private readonly IServiceProvider _services;

    public SaveSourceCodeFn(IServiceProvider services)
    {
        _services = services;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<SaveSourceCodeArgs>(message.FunctionArgs);

        var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "ai_generated_code");
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var path = Path.GetFullPath(dir, args.FilePath);
        var source = args.SourceCode;

        // Delete the file if it exists
        File.Delete(path);

        // Create a FileStream with sharing capabilities
        using FileStream fs = new FileStream(
            path,
            FileMode.OpenOrCreate,   // Create or overwrite the file
            FileAccess.ReadWrite, // Allow read and write operations
            FileShare.Read); // Allow other processes to read and write

        // Write some data to the file
        using StreamWriter writer = new StreamWriter(fs);
        writer.WriteLine(source);

        return true;
    }
}
