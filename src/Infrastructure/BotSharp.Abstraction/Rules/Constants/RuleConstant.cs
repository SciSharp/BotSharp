namespace BotSharp.Abstraction.Rules.Constants;

public static class RuleConstant
{
    public const int MAX_GRAPH_RECURSION = 1000;

    public const string INPUT_SCHEMA_KEY = "input_schema";
    public const string OUTPUT_SCHEMA_KEY = "output_schema";

    public static IEnumerable<string> CONDITION_NODE_TYPES = new List<string>
    {
        "condition",
        "criteria"
    };

    public static IEnumerable<string> ACTION_NODE_TYPES = new List<string>
    {
        "action"
    };

    public static IEnumerable<string> ROOT_NODE_TYPES = new List<string>
    {
        "root",
        "start"
    };

    public static IEnumerable<string> END_NODE_TYPES = new List<string>
    {
        "end",
        "terminal"
    };
}
