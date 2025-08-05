namespace BotSharp.Abstraction.SideCar.Models;

public class SideCarOptions
{
    public bool IsInheritStates { get; set; }
    public IEnumerable<string>? InheritStateKeys { get; set; }
    public IEnumerable<string>? ExcludedStateKeys { get; set; }

    public static SideCarOptions Empty()
    {
        return new();
    }

    public static SideCarOptions InheritStates(
        IEnumerable<string>? includedStates = null,
        IEnumerable<string>? excludedStates = null)
    {
        return new()
        {
            IsInheritStates = true,
            InheritStateKeys = includedStates,
            ExcludedStateKeys = excludedStates
        };
    }
}
