using System;

namespace BotSharp.Plugin.MultiTenancy.MultiTenancy;

public sealed class DisposeAction<T> : IDisposable
{
    private readonly Action<T> _action;
    private readonly T _state;

    public DisposeAction(Action<T> action, T state)
    {
        _action = action;
        _state = state;
    }

    public void Dispose()
    {
        _action?.Invoke(_state);
    }
}