using BotSharp.Plugin.MultiTenancy.Models;
using System.Threading;

namespace BotSharp.Plugin.MultiTenancy.MultiTenancy;

public class AsyncLocalCurrentTenantAccessor : ICurrentTenantAccessor
{
    private readonly AsyncLocal<TenantInfoBasic?> _currentScope;
    private AsyncLocalCurrentTenantAccessor()
    {
        _currentScope = new AsyncLocal<TenantInfoBasic?>();
    }

    public static AsyncLocalCurrentTenantAccessor Instance { get; } = new();

    public TenantInfoBasic? Current
    {
        get => _currentScope.Value;
        set => _currentScope.Value = value;
    }
}
