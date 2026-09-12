using System.Reflection;
using BotSharp.Abstraction.Plugins;
using BotSharp.Core.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BotSharp.Core.UnitTests.Plugins;

public class PluginLoaderTests
{
    // Regression test for https://github.com/SciSharp/BotSharp/issues/669:
    // PluginLoader used to derive its plugin search directory from
    // Assembly.GetEntryAssembly().Location. .NET leaves that empty when the app
    // is published as a self-contained single-file executable, which made
    // Directory.GetParent(string.Empty) throw an ArgumentException before the
    // app could even start. PluginLoader now uses AppContext.BaseDirectory,
    // which resolves correctly in that scenario too.
    [Fact]
    public void Load_does_not_throw_and_resolves_executing_dir_from_app_context_base_directory()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        var settings = new PluginSettings { Assemblies = [] };
        var loader = new PluginLoader(services, config, settings);

        var exception = Record.Exception(() => loader.Load(_ => { }));

        Assert.Null(exception);

        var executingDir = typeof(PluginLoader)
            .GetField("_executingDir", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetValue(null) as string;

        Assert.Equal(AppContext.BaseDirectory, executingDir);
    }

    [Fact]
    public void Load_finds_and_loads_plugin_assembly_from_executing_dir()
    {
        // BotSharp.Core.Rules is a real ProjectReference of this test project, so its
        // .dll is copied next to the test host's own assembly (AppContext.BaseDirectory) -
        // the exact directory PluginLoader now searches for plugin assemblies.
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        var settings = new PluginSettings { Assemblies = ["BotSharp.Core.Rules"] };
        var loader = new PluginLoader(services, config, settings);

        Assembly? loadedAssembly = null;
        loader.Load(assembly => loadedAssembly = assembly);

        Assert.NotNull(loadedAssembly);
        Assert.Equal("BotSharp.Core.Rules", loadedAssembly!.GetName().Name);
    }
}
