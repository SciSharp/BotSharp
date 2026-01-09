using BotSharp.Abstraction.Conversations.Enums;
using BotSharp.Abstraction.Users.Enums;
using BotSharp.Abstraction.Users.Models;

namespace BotSharp.Abstraction.Conversations.Extensions;

public static class ConversationExtensions
{
    /// <summary>
    /// Get whether the conversation can be accessed.
    /// </summary>
    /// <param name="conv">The conversation.</param>
    /// <param name="convUser">The user that created the conversation.</param>
    /// <param name="curUser">The current user that accesses the conversation.</param>
    /// <returns></returns>
    public static bool IsConversationAccessible(this Conversation conv, User? convUser, User? curUser)
    {
        if (conv == null || curUser == null)
        {
            return false;
        }

        if (convUser == null || UserConstant.AdminRoles.Contains(curUser.Role))
        {
            return true;
        }

        var result = false;
        var access = conv.Access ?? new();
        switch (access.AccessLevel)
        {
            case ConversationAccessLevel.Public:
                result = true;
                break;
            case ConversationAccessLevel.Private:
                result = convUser.Id.IsEqualTo(curUser.Id);
                break;
            case ConversationAccessLevel.Shared:
                if (access.AccessBy == ConversationAccessBy.User)
                {
                    result = access.Accessors?.Contains(curUser.Id) ?? false;
                }
                else if (access.AccessBy == ConversationAccessBy.UserRole)
                {
                    result = access.Accessors?.Contains(curUser.Role) ?? false;
                }
                break;
            default:
                break;
        }

        return result;
    }
}
