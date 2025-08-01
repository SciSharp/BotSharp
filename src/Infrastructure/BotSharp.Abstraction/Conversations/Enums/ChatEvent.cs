namespace BotSharp.Abstraction.Conversations.Enums;

public static class ChatEvent
{
    public const string OnConversationInitFromClient = nameof(OnConversationInitFromClient);
    public const string OnMessageReceivedFromClient = nameof(OnMessageReceivedFromClient);
    public const string OnMessageReceivedFromAssistant = nameof(OnMessageReceivedFromAssistant);

    public const string OnMessageDeleted = nameof(OnMessageDeleted);
    public const string OnNotificationGenerated = nameof(OnNotificationGenerated);
    public const string OnIndicationReceived = nameof(OnIndicationReceived);

    public const string OnConversationContentLogGenerated = nameof(OnConversationContentLogGenerated);
    public const string OnConversateStateLogGenerated = nameof(OnConversateStateLogGenerated);
    public const string OnAgentQueueChanged = nameof(OnAgentQueueChanged);
    public const string OnStateChangeGenerated = nameof(OnStateChangeGenerated);

    public const string BeforeReceiveLlmStreamMessage = nameof(BeforeReceiveLlmStreamMessage);
    public const string OnReceiveLlmStreamMessage = nameof(OnReceiveLlmStreamMessage);
    public const string AfterReceiveLlmStreamMessage = nameof(AfterReceiveLlmStreamMessage);
    public const string OnSenderActionGenerated = nameof(OnSenderActionGenerated);
}
