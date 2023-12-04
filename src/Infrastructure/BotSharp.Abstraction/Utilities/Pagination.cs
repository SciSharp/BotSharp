namespace BotSharp.Abstraction.Utilities;

public class Pagination
{
    public int Page { get; set; } = 0;
    /// <summary>
    /// Use -1 for all records
    /// </summary>
    public int Size { get; set; } = 20;
    public int Offset => (Page + 1) * Size;
}

public class PagedItems<T>
{
    public int Count { get; set; }
    public IEnumerable<T> Items { get; set; }
}
