using BotSharp.Abstraction.Messaging.JsonConverters;
using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Repositories.Enums;
using BotSharp.Abstraction.Users;
using BotSharp.Abstraction.Users.Models;
using BotSharp.Core;
using BotSharp.Logger;
using BotSharp.OpenAPI;
using BotSharp.Plugin.ChatHub;
using BotSharp.Plugin.EntityFrameworkCore;
using Serilog;

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

// ef test code for development
//if (app.Environment.IsDevelopment())
//{
//    var dbSettings = new BotSharpDatabaseSettings();
//    builder.Configuration.Bind("Database", dbSettings);
//    if (dbSettings.Default == RepositoryEnum.PostgreSqlRepository || dbSettings.Default == RepositoryEnum.MySqlRepository)
//    {
//        // Retrieve an instance of the DbContext class and manually run migrations during development
//        using (var scope = app.Services.CreateScope())
//        {
//            var context = scope.ServiceProvider.GetRequiredService<BotSharpEfCoreDbContext>();
//            context.Database.EnsureCreated();

//            if (!context.Users.Any())
//            {
//                // create a default user
//                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
//                await userService.CreateUser(new User
//                {
//                    UserName = "admin",
//                    FirstName = "Administrator",
//                    Email = "admin@gmail.com",
//                    Role = "admin",
//                    Password = "123456"
//                });
//            }
//        }
//    }
//}

// Enable SignalR
app.MapHub<SignalRHub>("/chatHub");
app.UseMiddleware<WebSocketsMiddleware>();

// Use BotSharp
app.UseBotSharp()
    .UseBotSharpOpenAPI(app.Environment)
    .UseBotSharpUI();

app.Run();