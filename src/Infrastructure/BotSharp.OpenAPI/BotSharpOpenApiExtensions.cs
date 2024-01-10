using Microsoft.AspNetCore.Builder;

namespace BotSharp.OpenAPI;

public static class BotSharpOpenApiExtensions
{
    /// <summary>
    /// Use Swagger/OpenAPI
    /// </summary>
    /// <param name="services"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseBotSharpOpenAPI(this IApplicationBuilder app, bool isDevelopment = false)
    {
        if (app == null)
        {
            throw new ArgumentNullException(nameof(app));
        }

        app.UseSwagger();
        if (isDevelopment)
        {
            app.UseSwaggerUI();
        }

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
