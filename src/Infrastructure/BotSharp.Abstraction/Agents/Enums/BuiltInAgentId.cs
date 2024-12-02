namespace BotSharp.Abstraction.Agents.Enums;

public class BuiltInAgentId
{
    /// <summary>
    /// A routing agent can be used as a base router.
    /// </summary>
    public const string AIAssistant = "01fcc3e5-9af7-49e6-ad7a-a760bd12dc4a";

    /// <summary>
    /// A demo agent used for open domain chatting
    /// </summary>
    public const string Chatbot = "01e2fc5c-2c89-4ec7-8470-7688608b496c";

    /// <summary>
    /// Human customer service
    /// </summary>
    public const string HumanSupport = "01dcc3e5-0af7-49e6-ad7a-a760bd12dc4b";

    /// <summary>
    /// Used as a container to host the shared tools/ utilities built in different plugins.
    /// </summary>
    public const string UtilityAssistant = "6745151e-6d46-4a02-8de4-1c4f21c7da95";

    /// <summary>
    /// Used when router can't route to any existing task agent
    /// </summary>
    public const string Fallback = "01fcc3e5-0af7-49e6-ad7a-a760bd12dc4d";

    /// <summary>
    /// Used by knowledgebase plugin to acquire domain knowledge
    /// </summary>
    public const string Learner = "01acc3e5-0af7-49e6-ad7a-a760bd12dc40";

    /// <summary>
    /// Plan feasible implementation steps for complex problems
    /// </summary>
    public const string Planner = "282a7128-69a1-44b0-878c-a9159b88f3b9";

    /// <summary>
    /// SQL statement generation
    /// </summary>
    public const string SqlDriver = "beda4c12-e1ec-4b4b-b328-3df4a6687c4f";

    /// <summary>
    /// Programming source code generation
    /// </summary>
    public const string CodeDriver = "c0ded7d9-3f9d-4ef6-b7ce-56a892dcef62";

    /// <summary>
    /// Evaluate prompt and conversation
    /// </summary>
    public const string Evaluator = "dfd9b46d-d00c-40af-8a75-3fbdc2b89869";

    /// <summary>
    /// Translates user-defined natural language rules into programmatic code
    /// </summary>
    public const string RuleEncoder = "6acfb93c-3412-402e-9ba5-c5d3cd8f0161";
}
