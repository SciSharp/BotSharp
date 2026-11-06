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
/// The agents the migration page can offer: those of the file repository, as id and name.
/// </summary>
public sealed class FileAgentOptionsTests : IDisposable
{
    private readonly string _repository = $"agent-options-{Guid.NewGuid():N}";
    private readonly string _root;

    public FileAgentOptionsTests()
    {
        _root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _repository);
        WriteAgent("id-b", "Beta");
        WriteAgent("id-a", "Alpha");
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private void WriteAgent(string id, string name)
    {
        var dir = Path.Combine(_root, "agents", id);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "agent.json"), $$"""{ "Id": "{{id}}", "Name": "{{name}}" }""");
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
        return new AgentService(services, new Mock<IBotSharpRepository>().Object,
            NullLogger<AgentService>.Instance, agentSettings, new BotSharpOptions());
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
}
