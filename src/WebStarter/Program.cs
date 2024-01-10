using BotSharp.Abstraction.Users;
using BotSharp.Core;
using BotSharp.OpenAPI;
using BotSharp.Core.Users.Services;
using BotSharp.Logger;
using BotSharp.Plugin.ChatHub;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

var loggerConfig = new LoggerConfiguration();
Log.Logger = loggerConfig
#if DEBUG
    .MinimumLevel.Debug()
#else
    .MinimumLevel.Warning()
#endif
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

builder.Services.AddScoped<IUserIdentity, UserIdentity>();
// Add bearer authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = false,
        ValidateIssuerSigningKey = true
    };
});

// Add BotSharp
builder.Services.AddBotSharpCore(builder.Configuration);
builder.Services.AddBotSharpOpenAPI(builder.Configuration);
builder.Services.AddBotSharpLogger(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("MyCorsPolicy",
        builder => builder.WithOrigins("http://localhost:5015",
                        "http://localhost:5500",
                        "https://botsharp.scisharpstack.org",
                        "https://chat.scisharpstack.org")
                   .AllowAnyMethod()
                   .AllowAnyHeader()
                   .AllowCredentials());
});

builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapHub<SignalRHub>("/chatHub");
app.UseChatHub();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Use BotSharp
app.UseBotSharp();
app.UseBotSharpOpenAPI();
app.UseBotSharpUI();

app.UseCors("MyCorsPolicy");

app.Run();
