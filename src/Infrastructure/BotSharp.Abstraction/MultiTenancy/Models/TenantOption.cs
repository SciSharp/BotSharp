namespace BotSharp.Abstraction.MultiTenancy.Models;

public sealed record TenantOption(Guid TenantId, string Name);