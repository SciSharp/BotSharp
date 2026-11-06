using BotSharp.Abstraction.Agents.Options;
using BotSharp.Abstraction.Agents.Settings;
using BotSharp.Abstraction.Conversations.Settings;
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
/// The migration page lists the agents of the file repository and sends the chosen ids back to RefreshAgents.
/// Both sides have to speak the id written in agent.json.
/// </summary>
public sealed class FileAgentOptionsTests : IDisposable
{
    private readonly string _repository = $"agent-options-{Guid.NewGuid():N}";
    private readonly string _root;
    private readonly Mock<IBotSharpRepository> _db = new();

    public FileAgentOptionsTests()
    {
        _root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _repository);
        WriteAgent("dir-b", """{ "Id": "id-b", "Name": "Beta" }""");
        WriteAgent("dir-a", """{ "Id": "id-a", "Name": "Alpha" }""");
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private void WriteAgent(string dirName, string json)
    {
        var dir = Path.Combine(_root, "agents", dirName);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "agent.json"), json);
    }

    private AgentService CreateService(string defaultRepository = RepositoryEnum.MongoRepository)
    {
        var settings = new BotSharpDatabaseSettings { Default = defaultRepository, FileRepository = _repository };
        var agentSettings = new AgentSettings();
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton(settings)
            .AddSingleton(agentSettings)
            .AddSingleton(new ConversationSetting())
            .BuildServiceProvider();
        return new AgentService(services, _db.Object, NullLogger<AgentService>.Instance, agentSettings, new BotSharpOptions());
    }

    [Fact]
    public async Task ListsIdAndNameOfEveryFileAgentSortedByName()
    {
        var options = await CreateService().GetFileAgentOptions();

        Assert.Equal(["id-a:Alpha", "id-b:Beta"], options.Select(x => $"{x.Id}:{x.Name}"));
    }

    [Fact]
    public async Task IsEmptyWhenMigrationIsNotAvailable()
    {
        Assert.Empty(await CreateService(RepositoryEnum.FileRepository).GetFileAgentOptions());
    }

    [Fact]
    public async Task DoesNotChangeTheSharedDatabaseSettings()
    {
        var service = CreateService();

        await service.GetFileAgentOptions();

        // FileRepository rewrites the settings it is built with; the shared instance has to stay as configured.
        Assert.Equal(_repository, service.GetDataDir());
    }

    [Fact]
    public async Task RefreshAgentsSelectsByAgentIdNotByDirectoryName()
    {
        var result = await CreateService().RefreshAgents(["dir-b"]);

        Assert.Equal("No agent gets migrated!", result);
        _db.Verify(x => x.DeleteAgent(It.IsAny<string>(), It.IsAny<AgentDeleteOptions?>()), Times.Never);
    }
}
