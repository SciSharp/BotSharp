using System;

namespace BotSharp.Plugin.MultiTenancy.Models;

public sealed record TenantInfoBasic(Guid? TenantId, string? Name);