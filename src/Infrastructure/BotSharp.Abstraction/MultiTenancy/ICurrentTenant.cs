namespace BotSharp.Abstraction.MultiTenancy;

public interface ICurrentTenant
{
    Guid? Id { get; }

    string? Name { get; }

    string? TenantId => Id?.ToString();

    IDisposable Change(Guid? id, string? name = null);
}