using BotSharp.Abstraction.Repositories.Enums;
using BotSharp.Core.Files.Services;
using Microsoft.Extensions.Configuration;

namespace BotSharp.Core.Files;

public class FileCorePlugin : IBotSharpPlugin
{
    public string Id => "6a8473c0-04eb-4346-be32-24755ce5973d";

    public string Name => "File Core";

    public string Description => "Provides file storage and analysis.";


    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        var fileCoreSettings = new FileCoreSettings();
        config.Bind("FileCore", fileCoreSettings);
        services.AddSingleton(fileCoreSettings);

        if (fileCoreSettings.Storage == FileStorageEnum.LocalFileStorage)
        {
            services.AddScoped<IFileStorageService, LocalFileStorageService>();
        }
        services.AddScoped<IFileInstructService, FileInstructService>();
    }
}
