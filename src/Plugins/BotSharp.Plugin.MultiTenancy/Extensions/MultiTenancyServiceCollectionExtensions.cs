using BotSharp.Abstraction.MultiTenancy;
using BotSharp.Abstraction.MultiTenancy.Options;
using BotSharp.Plugin.MultiTenancy.Models;
using BotSharp.Plugin.MultiTenancy.MultiTenancy;
using BotSharp.Plugin.MultiTenancy.MultiTenancy.Resolvers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BotSharp.Plugin.MultiTenancy.Extensions;

public static class MultiTenancyServiceCollectionExtensions
{
    public static IServiceCollection AddMultiTenancy(this IServiceCollection services, IConfiguration configuration, string sectionName = "TenantStore")
    {
        services.Configure<TenantResolveOptions>(options =>
        {
            options.TenantResolvers.Add(new ClaimsTenantResolveContributor());
            options.TenantResolvers.Add(new HeaderTenantResolveContributor());
            options.TenantResolvers.Add(new QueryStringTenantResolveContributor());
        });

        services.Configure<TenantStoreOptions>(configuration.GetSection(sectionName));
        services.AddScoped<ICurrentTenant, CurrentTenant>();
        services.AddSingleton<ICurrentTenantAccessor>(AsyncLocalCurrentTenantAccessor.Instance);
        services.AddScoped<ITenantResolver, TenantResolver>();
        services.AddScoped<IConnectionStringResolver, DefaultConnectionStringResolver>();
        services.AddScoped<ITenantConnectionProvider, TenantConnectionProvider>();
        services.AddSingleton<ITenantFeature, TenantFeature>();
        services.AddScoped<MultiTenancyMiddleware>();

        services.TryAddScoped<ITenantOptionProvider, ConfigTenantOptionProvider>();

        return services;
    }
}