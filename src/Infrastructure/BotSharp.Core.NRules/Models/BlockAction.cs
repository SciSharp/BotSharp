namespace BotSharp.Core.NRules.Models;

public class BlockAction
{
    public string Reason { get; internal set; }
    public bool StopCompletion { get; internal set; }
}