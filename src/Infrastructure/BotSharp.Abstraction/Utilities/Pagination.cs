using BotSharp.Abstraction.Infrastructures;

namespace BotSharp.Abstraction.Utilities;

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
        => $"{_page}_{_size}_{Sort}_{Order}";
}

public class PagedItems<T>
{
    public int Count { get; set; }
    public IEnumerable<T> Items { get; set; } = new List<T>();
}
