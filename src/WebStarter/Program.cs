using BotSharp.Abstraction.Users;
using BotSharp.Core;
using BotSharp.OpenAPI;
using BotSharp.Core.Users.Services;
using BotSharp.Logger;
using BotSharp.Plugin.ChatHub;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IUserIdentity, UserIdentity>();

// Add BotSharp
builder.Services.AddBotSharpCore(builder.Configuration);
builder.Services.AddBotSharpOpenAPI(builder.Configuration);
builder.Services.AddBotSharpLogger(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("MyCorsPolicy",
        builder => builder.WithOrigins("http://localhost:5010")
                   .AllowAnyMethod()
                   .AllowAnyHeader()
                   .AllowCredentials());
});

builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHub<SignalRHub>("/chatHub");
app.UseMiddleware<WebSocketsMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Use BotSharp
app.UseBotSharp();

#if DEBUG
app.UseCors("MyCorsPolicy");
#endif

app.Run();
