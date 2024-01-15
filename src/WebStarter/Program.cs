using BotSharp.Core;
using BotSharp.OpenAPI;
using BotSharp.Logger;
using BotSharp.Plugin.ChatHub;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Use Serilog
Log.Logger = new LoggerConfiguration()
#if DEBUG
    .MinimumLevel.Information()
#else
    .MinimumLevel.Warning()
#endif
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", 
        shared: true, 
        rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

// Add BotSharp
builder.Services.AddBotSharpCore(builder.Configuration)
    .AddBotSharpOpenAPI(builder.Configuration, new[]
    {
        "http://localhost:5015",
        "https://botsharp.scisharpstack.org",
        "https://chat.scisharpstack.org"
    }, builder.Environment, true)
    .AddBotSharpLogger(builder.Configuration);

// Add SignalR for WebSocket
builder.Services.AddSignalR();

var app = builder.Build();

// Enable SignalR
app.MapHub<SignalRHub>("/chatHub");
app.UseMiddleware<WebSocketsMiddleware>();

// Use BotSharp
app.UseBotSharp()
    .UseBotSharpOpenAPI(app.Environment)
    .UseBotSharpUI();

app.Run();
