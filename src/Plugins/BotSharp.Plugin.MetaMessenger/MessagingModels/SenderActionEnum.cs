namespace BotSharp.Plugin.MetaMessenger.MessagingModels;

public enum SenderActionEnum
{
    [EnumMember(Value = "typing_on")]
    TypingOn,
    [EnumMember(Value = "typing_off")]
    TypingOff,
    [EnumMember(Value = "mark_seen")]
    MarkSeen
}
