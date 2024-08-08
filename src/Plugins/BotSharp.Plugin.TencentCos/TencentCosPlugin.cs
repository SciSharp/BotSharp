using BotSharp.Abstraction.Files;
using BotSharp.Abstraction.Repositories.Enums;
using BotSharp.Abstraction.Settings;
using BotSharp.Plugin.TencentCos;
using BotSharp.Plugin.TencentCos.Services;
using BotSharp.Plugin.TencentCos.Settings;

namespace BotSharp.Plugin.TencentCosFile.Files;

public class TencentCosPlugin : IBotSharpPlugin
{
    public string Id => "3f55b702-8a28-4f9a-907c-affc24f845f1";

    public string Name => "TencentCos";

    public string Description => "Provides connection to Tencent Cloud object storage service.";


    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        var myFileStorageSettings = new FileStorageSettings();
        config.Bind("FileStorage", myFileStorageSettings);

        if (myFileStorageSettings.Default == FileStorageEnum.TencentCosStorage)
        {
            services.AddScoped(provider =>
            {
                var settingService = provider.GetRequiredService<ISettingService>();
                return settingService.Bind<TencentCosSettings>("TencentCos");
            });

            services.AddScoped<TencentCosClient>();
            services.AddScoped<IFileStorageService, TencentCosService>();
        }
    }
}
