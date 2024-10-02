using BotSharp.Core;
using BotSharp.OpenAPI;
using BotSharp.Logger;
using BotSharp.Plugin.ChatHub;
using Serilog;
using BotSharp.Abstraction.Messaging.JsonConverters;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

string[] allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? new[]
    {
        "http://0.0.0.0:5015",
        "https://botsharp.scisharpstack.org",
        "https://chat.scisharpstack.org"
    };
 
 // Add BotSharp
 builder.Services.AddBotSharpCore(builder.Configuration, options =>
 {
     options.JsonSerializerOptions.Converters.Add(new RichContentJsonConverter());
     options.JsonSerializerOptions.Converters.Add(new TemplateMessageJsonConverter());
 }).AddBotSharpOpenAPI(builder.Configuration, allowedOrigins, builder.Environment, true)
   .AddBotSharpLogger(builder.Configuration);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

// Add SignalR for WebSocket
builder.Services.AddSignalR()
    // Enable Redis backplane for SignalR
    /*.AddStackExchangeRedis("127.0.0.1", o =>
    {
        o.Configuration.ChannelPrefix = RedisChannel.Literal("botsharp");
    })*/;

var app = builder.Build();

// Enable SignalR
app.MapHub<SignalRHub>("/chatHub");
app.UseMiddleware<WebSocketsMiddleware>();

// Use BotSharp
app.UseBotSharp()
    .UseBotSharpOpenAPI(app.Environment)
    .UseBotSharpUI();

app.Run();