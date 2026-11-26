using System.Net;
using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Enums;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Files;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.MessageHub.Models;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.MLTasks.Settings;
using BotSharp.Core.MessageHub;
using BotSharp.Plugin.OpenAI.Providers.Chat;
using BotSharp.Plugin.OpenAI.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BotSharp.Core.UnitTests.Routing;

public class OpenAIResponsesStreamingTests
{
    [Fact]
    public async Task FailedResponsePublishesFailureAfterPartialText()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
        await using var app = builder.Build();
        app.MapPost("/responses", async context =>
        {
            context.Response.ContentType = "text/event-stream";
            await context.Response.WriteAsync("""
                event: response.output_text.delta
                data: {"type":"response.output_text.delta","item_id":"msg_1","output_index":0,"content_index":0,"sequence_number":1,"delta":"Partial answer"}

                event: response.failed
                data: {"type":"response.failed","sequence_number":2,"response":{"id":"resp_1","object":"response","created_at":0,"status":"failed","model":"test-model","output":[],"error":{"code":"server_error","message":"Test failure"}}}


                """);
        });
        await app.StartAsync();

        var agent = new Agent { Id = "test-agent" };
        var agentService = new Mock<IAgentService>();
        agentService.Setup(service => service.PrepareInstructionAndFunctions(agent, It.IsAny<IDictionary<string, object>?>(), null))
            .Returns((string.Empty, Array.Empty<FunctionDef>()));

        var settings = new Mock<ILlmProviderService>();
        settings.Setup(service => service.GetSetting("openai", "test-model"))
            .Returns(new LlmModelSetting { Endpoint = app.Urls.Single() });

        var hub = new MessageHub<HubObserveData<RoleDialogModel>>(NullLogger<MessageHub<HubObserveData<RoleDialogModel>>>.Instance);
        var events = new List<HubObserveData<RoleDialogModel>>();
        using var subscription = hub.Events.Subscribe(events.Add);
        using var services = new ServiceCollection()
            .AddSingleton(hub)
            .AddSingleton(agentService.Object)
            .AddSingleton(settings.Object)
            .AddSingleton(Mock.Of<IConversationService>(service => service.ConversationId == "test-conversation"))
            .AddSingleton(Mock.Of<IConversationCancellationService>())
            .BuildServiceProvider();

        var provider = new ChatCompletionProvider(
            new OpenAiSettings { UseResponseApi = true },
            NullLogger<ChatCompletionProvider>.Instance,
            services,
            Mock.Of<IConversationStateService>(),
            Mock.Of<IFileStorageService>());
        provider.SetModelName("test-model");
        provider.SetApiKey("test-key");

        var result = await provider.GetChatCompletionsStreamingAsync(agent, []);

        Assert.Equal("Partial answer", Assert.Single(events, e => e.EventName == ChatEvent.OnReceiveLlmStreamMessage).Data!.Content);
        var final = Assert.Single(events, e => e.EventName == ChatEvent.AfterReceiveLlmStreamMessage);
        Assert.Same(result, final.Data);
        Assert.True(result.StopCompletion);
        Assert.Equal("AI response failed", result.Content);
    }
}
