namespace BotSharp.Core.Rules.Constants;

public static class RuleConstant
{
    public const string DEFAULT_CRITERIA_PROVIDER = "code_script";
    public const int MAX_GRAPH_RECURSION = 10;

    public static IEnumerable<string> CONDITION_NODE_TYPES = new List<string>
    {
        "condition",
        "criteria"
    };

    public static IEnumerable<string> ACTION_NODE_TYPES = new List<string>
    {
        "action"
    };

    public static IEnumerable<string> END_NODE_TYPES = new List<string>
    {
        "root",
        "start",
        "end"
    };
}
