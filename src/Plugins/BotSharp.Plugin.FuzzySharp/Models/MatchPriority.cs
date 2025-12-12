namespace BotSharp.Plugin.FuzzySharp.Models;

public class MatchPriority
{
    public int Order { get; set; }
    public string Name { get; set; } = string.Empty;

    public MatchPriority()
    {
        
    }

    public MatchPriority(int order, string name)
    {
        Order = order;
        Name = name;
    }

    public override string ToString()
    {
        return $"{Name} => {Order}";
        ;
    }
}
