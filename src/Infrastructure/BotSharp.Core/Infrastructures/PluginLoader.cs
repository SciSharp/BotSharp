using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace BotSharp.Core.Infrastructures;

public class PluginLoader
{
    private readonly IServiceCollection _services;
    private readonly IConfiguration _config;
    private readonly PluginLoaderSettings _settings;
    private static List<IBotSharpPlugin> _modules = new List<IBotSharpPlugin>();

    public PluginLoader(IServiceCollection services,
        IConfiguration config,
        PluginLoaderSettings settings)
    {
        _services = services;
        _config = config;
        _settings = settings;
    }

    public void Load(Action<Assembly> loaded)
    {
        var executingDir = Directory.GetParent(Assembly.GetEntryAssembly().Location).FullName;

        _settings.Assemblies.ToList().ForEach(assemblyName =>
        {
            var assemblyPath = Path.Combine(executingDir, assemblyName + ".dll");
            if (File.Exists(assemblyPath))
            {
                var assembly = Assembly.Load(assemblyName);

                var modules = assembly.GetTypes()
                    .Where(x => x.GetInterface(nameof(IBotSharpPlugin)) != null)
                    .Select(x => Activator.CreateInstance(x) as IBotSharpPlugin)
                    .ToList();

                foreach (var module in modules)
                {
                    module.RegisterDI(_services, _config);
                    Console.WriteLine($"Loaded plugin {module.GetType().Name} from {assemblyName}.", Color.Green);
                }

                loaded(assembly);
                _modules.AddRange(modules);
            }
            else
            {
                Console.WriteLine($"Can't find assemble {assemblyPath}.");
            }
        });
    }

    public void Configure(IApplicationBuilder app)
    {
        if (_modules.Count == 0)
        {
            Console.WriteLine($"No plugin loaded. Please check whether the Load() method is called.", Color.Yellow);
        }

        _modules.ForEach(module =>
        {
            if (module.GetType().GetInterface(nameof(IBotSharpAppPlugin)) != null)
            {
                (module as IBotSharpAppPlugin).Configure(app);
            }
        });
    }
}
