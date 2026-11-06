using BotSharp.Abstraction.Agents.Settings;
using BotSharp.Abstraction.Options;
using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Repositories.Enums;
using BotSharp.Abstraction.Repositories.Settings;
using BotSharp.Core.Agents.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BotSharp.Core.UnitTests.Agents;

/// <summary>
/// The migration page sends the listed ids back to RefreshAgents, which filters on the agent directory
/// name, so that is the id that has to be listed.
/// </summary>
public sealed class FileAgentOptionsTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private void WriteAgent(string dirName, string json)
    {
        var dir = Path.Combine(_root, "agents", dirName);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "agent.json"), json);
    }

    private AgentService CreateService(string defaultRepository = RepositoryEnum.MongoRepository)
    {
        var settings = new BotSharpDatabaseSettings { Default = defaultRepository, FileRepository = _root };
        var services = new ServiceCollection().AddSingleton(settings).BuildServiceProvider();
        return new AgentService(services, new Mock<IBotSharpRepository>().Object,
            NullLogger<AgentService>.Instance, new AgentSettings(), new BotSharpOptions());
    }

    [Fact]
    public async Task ListsDirectoryNameAsIdAndSortsByName()
    {
        WriteAgent("dir-b", """{ "Id": "other-id", "Name": "Beta" }""");
        WriteAgent("dir-a", """{ "name": "Alpha" }""");

        var options = await CreateService().GetFileAgentOptions();

        Assert.Equal(["dir-a:Alpha", "dir-b:Beta"], options.Select(x => $"{x.Id}:{x.Name}"));
    }

    [Fact]
    public async Task SkipsDirectoriesWithoutAValidAgentFile()
    {
        WriteAgent("valid", """{ "name": "Valid" }""");
        WriteAgent("broken", "{ not json");
        Directory.CreateDirectory(Path.Combine(_root, "agents", "empty"));

        var options = await CreateService().GetFileAgentOptions();

        Assert.Equal(["valid"], options.Select(x => x.Id));
    }

    [Fact]
    public async Task IsEmptyWhenMigrationIsNotAvailable()
    {
        WriteAgent("valid", """{ "name": "Valid" }""");

        Assert.Empty(await CreateService(RepositoryEnum.FileRepository).GetFileAgentOptions());
    }
}
