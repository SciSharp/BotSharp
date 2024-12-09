using BotSharp.Abstraction.Infrastructures;
using System.Diagnostics;

namespace BotSharp.Abstraction.Utilities;

[DebuggerStepThrough]
public class Pagination : ICacheKey
{
    private int _page;
    private int _size;

    public int Page
    {
        get { return _page > 0 ? _page : 1; }
        set { _page = value; }
    }

    public int Size
    {
        get
        {
            return _size > 0 ? _size : 1;
        }
        set
        {
            _size = value;
        }
    }

    /// <summary>
    /// Sort by field
    /// </summary>
    public string? Sort { get; set; }

    /// <summary>
    /// Sort order: asc or desc
    /// </summary>
    public string Order { get; set; } = "asc";

    public int Offset
    {
        get { return (Page - 1) * Size; }
    }

    public bool ReturnTotal { get; set; } = true;

    public string GetCacheKey()
        => $"{nameof(Pagination)}_{_page}_{_size}_{Sort}_{Order}_{Offset}_{ReturnTotal}";
}

public class PagedItems<T>
{
    public int Count { get; set; }
    public IEnumerable<T> Items { get; set; } = new List<T>();
}
