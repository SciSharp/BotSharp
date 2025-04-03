using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol.Messages;
using ModelContextProtocol.Server;
using ModelContextProtocol.Utils.Json;

namespace BotSharp.PizzaBot.MCPServer;

public static class McpEndpointRouteBuilderExtensions
{
    public static IEndpointConventionBuilder MapMcpSse(this IEndpointRouteBuilder endpoints)
    {
        IMcpServer? server = null;
        SseServerStreamTransport? transport = null;
        var loggerFactory = endpoints.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var mcpServerOptions = endpoints.ServiceProvider.GetRequiredService<IOptions<McpServerOptions>>();

        var routeGroup = endpoints.MapGroup("");

        routeGroup.MapGet("/sse", async (HttpResponse response, CancellationToken requestAborted) =>
        {
            await using var localTransport = transport = new SseServerStreamTransport(response.Body);
            await using var localServer = server = McpServerFactory.Create(transport, mcpServerOptions.Value, loggerFactory, endpoints.ServiceProvider);

            await localServer.StartAsync(requestAborted);

            response.Headers.ContentType = "text/event-stream";
            response.Headers.CacheControl = "no-cache";

            try
            {
                await transport.RunAsync(requestAborted);
            }
            catch (OperationCanceledException) when (requestAborted.IsCancellationRequested)
            {
                // RequestAborted always triggers when the client disconnects before a complete response body is written,
                // but this is how SSE connections are typically closed.
            }
        });

        routeGroup.MapPost("/message", async (HttpContext context) =>
        {
            if (transport is null)
            {
                await Results.BadRequest("Connect to the /sse endpoint before sending messages.").ExecuteAsync(context);
                return;
            }

            var message = await context.Request.ReadFromJsonAsync<IJsonRpcMessage>(McpJsonUtilities.DefaultOptions, context.RequestAborted);
            if (message is null)
            {
                await Results.BadRequest("No message in request body.").ExecuteAsync(context);
                return;
            }

            await transport.OnMessageReceivedAsync(message, context.RequestAborted);
            context.Response.StatusCode = StatusCodes.Status202Accepted;
            await context.Response.WriteAsync("Accepted");
        });

        return routeGroup;
    }
}
