using BotSharp.Abstraction.MultiTenancy.Models;
using System.Threading.Tasks;

namespace BotSharp.Plugin.MultiTenancy.MultiTenancy;

public interface ITenantResolver
{
    Task<TenantResolveResult> ResolveAsync(TenantResolveContext context);
}