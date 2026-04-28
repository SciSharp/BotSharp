namespace BotSharp.Core.Rules.Engines;

/// <summary>
/// Abstraction over the data structure that drives graph traversal order.
/// Stack → DFS, Queue → BFS.  Swap the frontier mid-traversal to switch strategy.
/// </summary>
public interface IFrontier<T>
{
    void Add(T item);
    T Remove();
    int Count { get; }

    /// <summary>
    /// Drain every remaining item into <paramref name="other"/>, preserving order.
    /// </summary>
    void DrainTo(IFrontier<T> other);
}

/// <summary>
/// LIFO frontier – produces depth-first traversal.
/// </summary>
public sealed class StackFrontier<T> : IFrontier<T>
{
    private readonly Stack<T> _stack = new();

    public int Count => _stack.Count;

    public void Add(T item) => _stack.Push(item);

    public T Remove() => _stack.Pop();

    public void DrainTo(IFrontier<T> other)
    {
        // Pop gives items in priority order (highest-weight first).
        var items = new List<T>();
        while (_stack.Count > 0)
        {
            items.Add(_stack.Pop());
        }

        if (other is StackFrontier<T>)
        {
            items.Reverse();
        }

        foreach (var item in items)
        {
            other.Add(item);
        }
    }
}

/// <summary>
/// FIFO frontier – produces breadth-first traversal.
/// </summary>
public sealed class QueueFrontier<T> : IFrontier<T>
{
    private readonly Queue<T> _queue = new();

    public int Count => _queue.Count;

    public void Add(T item) => _queue.Enqueue(item);

    public T Remove() => _queue.Dequeue();

    public void DrainTo(IFrontier<T> other)
    {
        // Dequeue gives items in priority order (highest-weight first).
        var items = new List<T>();
        while (_queue.Count > 0)
        {
            items.Add(_queue.Dequeue());
        }

        if (other is StackFrontier<T>)
        {
            items.Reverse();
        }

        foreach (var item in items)
        {
            other.Add(item);
        }
    }
}
