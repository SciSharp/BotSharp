using BotSharp.Abstraction.MultiTenancy;
using System.Collections.Generic;

namespace BotSharp.Plugin.MultiTenancy.Models;

public class TenantResolveOptions
{
    public List<ITenantResolveContributor> TenantResolvers { get; set; } = new();
}