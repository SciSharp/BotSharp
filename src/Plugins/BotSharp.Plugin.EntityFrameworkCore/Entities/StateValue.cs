namespace BotSharp.Plugin.EntityFrameworkCore.Entities;

public class StateValue
{
    public string Id { get; set; }
    public string Data { get; set; }
    public string? MessageId { get; set; }
    public bool Active { get; set; }
    public int ActiveRounds { get; set; }
    public string DataType { get; set; }
    public string Source { get; set; }
    public DateTime UpdateTime { get; set; }
    public string StateId { get; set; }
    public State State { get; set; }
}
