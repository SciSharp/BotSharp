using BotSharp.Abstraction.Messaging.JsonConverters;
using BotSharp.Core.Users.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace BotSharp.OpenAPI;

public static class BotSharpOpenApiExtensions
{
    private static string policy = "BotSharpCorsPolicy";
    /// <summary>
    /// Add Swagger/OpenAPI
    /// </summary>
    /// <param name="services"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    public static IServiceCollection AddBotSharpOpenAPI(this IServiceCollection services, 
        IConfiguration config, 
        string[] origins,
        IHostEnvironment env,
        bool enableValidation)
    {
        services.AddScoped<IUserIdentity, UserIdentity>();

        // Add bearer authentication
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(o =>
        {
            o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer = config["Jwt:Issuer"],
                ValidAudience = config["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"])),
                ValidateIssuer = enableValidation,
                ValidateAudience = enableValidation,
                ValidateLifetime = enableValidation && !env.IsDevelopment(),
                ValidateIssuerSigningKey = enableValidation
            };

            if (!enableValidation)
            {
                o.TokenValidationParameters.SignatureValidator = (string token, TokenValidationParameters parameters) =>
                    new JwtSecurityToken(token);
            }
        });

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

        services.AddCors(options =>
        {
            options.AddPolicy(policy,
                builder => builder.WithOrigins(origins)
                           .AllowAnyMethod()
                           .AllowAnyHeader()
                           .AllowCredentials());
        });

        return services;
    }

    /// <summary>
    /// Use Swagger/OpenAPI
    /// </summary>
    /// <param name="services"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseBotSharpOpenAPI(this IApplicationBuilder app, IHostEnvironment env)
    {
        if (app == null)
        {
            throw new ArgumentNullException(nameof(app));
        }

        app.UseCors(policy);

        app.UseSwagger();
        if (env.IsDevelopment())
        {
            app.UseSwaggerUI();
            app.UseDeveloperExceptionPage();
        }

        app.UseAuthentication();

        app.UseRouting();
        
        app.UseAuthorization();

        app.UseEndpoints(
            endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
            });

        return app;
    }

    /// <summary>
    /// Host BotSharp UI built in adapter-static
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static IApplicationBuilder UseBotSharpUI(this IApplicationBuilder app, bool isDevelopment = false)
    {
        if (app == null)
        {
            throw new ArgumentNullException(nameof(app));
        }

        // app.UseFileServer();
        app.UseDefaultFiles();
        app.UseStaticFiles();

        app.UseEndpoints(
            endpoints =>
            {
                // For SPA static file routing
                endpoints.MapFallbackToFile("/index.html");
            });

        app.UseSpa(config =>
        {
            if (isDevelopment)
            {
                config.UseProxyToSpaDevelopmentServer("http://localhost:5015");
            }
        });

        return app;
    }
}
