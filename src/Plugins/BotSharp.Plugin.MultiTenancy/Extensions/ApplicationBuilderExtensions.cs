using BotSharp.Plugin.MultiTenancy.MultiTenancy;
using Microsoft.AspNetCore.Builder;

namespace BotSharp.Plugin.MultiTenancy.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseMultiTenancy(this IApplicationBuilder app)
    {
        return app.UseMiddleware<MultiTenancyMiddleware>();
    }
}