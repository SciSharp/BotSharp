namespace BotSharp.Abstraction.Models;

public class SuccessFailResponse<T>
{
    public List<T> Success { get; set; } = [];
    public List<T> Fail { get; set; } = [];
}
