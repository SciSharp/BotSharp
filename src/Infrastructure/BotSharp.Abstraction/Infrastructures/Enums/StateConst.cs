namespace BotSharp.Abstraction.Infrastructures.Enums;

public class StateConst
{
    public const string EXPECTED_ACTION_AGENT = "expected_next_action_agent";
    public const string EXPECTED_GOAL_AGENT = "expected_user_goal_agent";
    public const string NEXT_ACTION_AGENT = "next_action_agent";
    public const string NEXT_ACTION_REASON = "next_action_reason";
    public const string USER_GOAL_AGENT = "user_goal_agent";
    public const string AGENT_REDIRECTION_REASON = "agent_redirection_reason";
    // lazy or eager
    public const string ROUTING_MODE = "routing_mode";
    public const string LAZY_ROUTING_AGENT_ID = "lazy_routing_agent_id";

    public const string LANGUAGE = "language";

    public const string SUB_CONVERSATION_ID = "sub_conversation_id";
    public const string ORIGIN_CONVERSATION_ID = "origin_conversation_id";
    public const string WEB_DRIVER_TASK_ID = "web_driver_task_id";
}
