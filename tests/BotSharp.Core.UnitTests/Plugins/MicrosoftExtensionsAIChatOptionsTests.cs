using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Plugin.MicrosoftExtensionsAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BotSharp.Core.UnitTests.Plugins;

public class MicrosoftExtensionsAIChatOptionsTests
{
    private sealed class CapturingChatClient : IChatClient
    {
        public ChatOptions? Options { get; private set; }

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            Options = options;
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose() { }
    }

    private static async Task<ChatOptions?> Run(string? temperature, string? maxTokens, bool withFunction)
    {
        var state = new Mock<IConversationStateService>();
        state.Setup(x => x.GetState("temperature", It.IsAny<string>())).Returns(temperature ?? string.Empty);
        state.Setup(x => x.GetState("max_tokens", It.IsAny<string>())).Returns(maxTokens ?? string.Empty);

        var function = new FunctionDef { Name = "get_weather", Description = "weather" };
        var agentService = new Mock<IAgentService>();
        agentService.Setup(x => x.CollectRenderData(It.IsAny<Agent>())).Returns(new Dictionary<string, object>());
        agentService.Setup(x => x.PrepareInstructionAndFunctions(It.IsAny<Agent>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<StringComparer>()))
            .Returns(("be brief", withFunction ? new[] { function } : Array.Empty<FunctionDef>()));
        agentService.Setup(x => x.RenderFunction(It.IsAny<Agent>(), It.IsAny<FunctionDef>(), It.IsAny<IDictionary<string, object>>())).Returns(true);
        agentService.Setup(x => x.RenderFunctionProperty(It.IsAny<Agent>(), It.IsAny<FunctionDef>(), It.IsAny<IDictionary<string, object>>()))
            .Returns(new FunctionParametersDef());

        var services = new ServiceCollection();
        services.AddSingleton(state.Object);
        services.AddSingleton(agentService.Object);
        var client = new CapturingChatClient();
        var provider = new MicrosoftExtensionsAIChatCompletionProvider(
            client, NullLogger<MicrosoftExtensionsAIChatCompletionProvider>.Instance, services.BuildServiceProvider());

        await provider.GetChatCompletions(new Agent { Id = "a", Name = "a" }, [new RoleDialogModel("user", "hi") { CurrentAgentId = "a" }]);
        return client.Options;
    }

    [Fact]
    public async Task Temperature_and_max_tokens_from_state_reach_the_client()
    {
        var options = await Run("0.7", "256", withFunction: false);

        Assert.NotNull(options);
        Assert.Equal(0.7f, options!.Temperature);
        Assert.Equal(256, options.MaxOutputTokens);
    }

    [Fact]
    public async Task Rendered_functions_reach_the_client_as_tools()
    {
        var options = await Run(null, null, withFunction: true);

        Assert.NotNull(options);
        var tool = Assert.Single(options!.Tools!);
        Assert.Equal("get_weather", tool.Name);
    }
}
