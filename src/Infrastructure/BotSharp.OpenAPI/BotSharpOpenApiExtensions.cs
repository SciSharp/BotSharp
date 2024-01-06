using BotSharp.Abstraction.Messaging.JsonConverters;
using Microsoft.Extensions.Configuration;

namespace BotSharp.OpenAPI;

public static class BotSharpOpenApiExtensions
{
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
}
