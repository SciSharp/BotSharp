using BotSharp.Abstraction.Repositories.Enums;
using BotSharp.Core.Files.Services;
using Microsoft.Extensions.Configuration;

namespace BotSharp.Core.Files;

public class FilePlugin : IBotSharpPlugin
{
    public string Id => "6a8473c0-04eb-4346-be32-24755ce5973d";

    public string Name => "File";

    public string Description => "Provides file analysis.";


    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        var myFileStorageSettings = new FileStorageSettings();
        config.Bind("FileStorage", myFileStorageSettings);

        if (myFileStorageSettings.Default == FileStorageEnum.LocalFileStorage)
        {
            services.AddScoped<IBotSharpFileService, BotSharpFileService>();
        }
    }
}
