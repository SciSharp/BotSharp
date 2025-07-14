namespace BotSharp.Abstraction.SideCar.Models;

public class SideCarOptions
{
    public bool IsInheritStates { get; set; }
    public IEnumerable<string>? InheritStateKeys { get; set; }

    public static SideCarOptions Empty()
    {
        return new();
    }

    public static SideCarOptions InheritStates(IEnumerable<string>? targetStates = null)
    {
        return new()
        {
            IsInheritStates = true,
            InheritStateKeys = targetStates
        };
    }
}
