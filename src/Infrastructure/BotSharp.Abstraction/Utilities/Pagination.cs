namespace BotSharp.Abstraction.Utilities;

public class Pagination
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
            if (_size <= 0) return 20;
            if (_size > 100) return 100;

            return _size;
        } 
        set 
        {
            _size = value;
        } 
    }

    public int Offset
    {
        get { return (Page - 1) * Size; }
    }
}

public class PagedItems<T>
{
    public int Count { get; set; }
    public IEnumerable<T> Items { get; set; } = new List<T>();
}
