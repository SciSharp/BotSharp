namespace BotSharp.Abstraction.SideCar.Models;

public class SideCarOptions
{
    public bool IsInheritStates { get; set; }
    public HashSet<string>? InheritStateKeys { get; set; }
    public HashSet<string>? ExcludedStateKeys { get; set; }

    public static SideCarOptions Empty()
    {
        return new();
    }

    public static SideCarOptions InheritStates(
        HashSet<string>? includedStates = null,
        HashSet<string>? excludedStates = null)
    {
        return new()
        {
            IsInheritStates = true,
            InheritStateKeys = includedStates,
            ExcludedStateKeys = excludedStates
        };
    }
}
