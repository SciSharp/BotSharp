namespace BotSharp.Core.Rules.Constants;

public static class RuleConstant
{
    public const int MAX_GRAPH_RECURSION = 50;

    public static IEnumerable<string> CONDITION_NODE_TYPES = new List<string>
    {
        "condition",
        "criteria"
    };

    public static IEnumerable<string> ACTION_NODE_TYPES = new List<string>
    {
        "action"
    };
}
