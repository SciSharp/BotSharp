using BotSharp.Abstraction.Repositories.Models;

namespace BotSharp.Abstraction.Conversations.Models;

public class ConversationState : Dictionary<string, string>
{
    public ConversationState()
    {
        
    }

    public ConversationState(List<KeyValueModel> pairs)
    {
        foreach (var pair in pairs)
        {
            this[pair.Key] = pair.Value;
        }
    }
}
