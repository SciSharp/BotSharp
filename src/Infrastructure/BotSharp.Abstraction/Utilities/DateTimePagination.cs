namespace BotSharp.Abstraction.Utilities;

public class DateTimePagination<T> : PagedItems<T>
{
    public DateTime? NextTime { get; set; }
}
