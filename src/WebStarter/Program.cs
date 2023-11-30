using BotSharp.Abstraction.Messaging;
using BotSharp.Abstraction.Users;
using BotSharp.Core;
using BotSharp.Core.Users.Services;
using BotSharp.Logger;
using BotSharp.Plugin.ChatHub;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new RichContentJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new TemplateMessageJsonConverter());
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpContextAccessor();

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

builder.Services.AddScoped<IUserIdentity, UserIdentity>();

// Add BotSharp
builder.Services.AddBotSharpCore(builder.Configuration);
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
