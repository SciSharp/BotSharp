using BotSharp.Abstraction.Messaging.JsonConverters;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace BotSharp.Plugin.ChatHub;

public static class ChatHubServiceExtensions
{
    /// <summary>
    /// Add Swagger/OpenAPI
    /// </summary>
    /// <param name="services"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    public static IServiceCollection AddBotSharpOpenAPI(this IServiceCollection services, IConfiguration config)
    {
        // Add services to the container.
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new RichContentJsonConverter());
                options.JsonSerializerOptions.Converters.Add(new TemplateMessageJsonConverter());
            });

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        services.AddHttpContextAccessor();

        return services;
    }

    /// <summary>
    /// Host BotSharp UI built in adapter-static
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static IApplicationBuilder UseChatHub(this IApplicationBuilder app)
    {
        if (app == null)
        {
            throw new ArgumentNullException(nameof(app));
        }

        app.UseMiddleware<WebSocketsMiddleware>();

        return app;
    }
}
