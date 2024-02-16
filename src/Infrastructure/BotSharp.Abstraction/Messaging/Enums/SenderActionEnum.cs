using System.Runtime.Serialization;

namespace BotSharp.Abstraction.Messaging.Enums;

public enum SenderActionEnum
{
    [EnumMember(Value = "typing_on")]
    TypingOn = 1,
    [EnumMember(Value = "typing_off")]
    TypingOff,
    [EnumMember(Value = "mark_seen")]
    MarkSeen
}
