namespace BotSharp.Abstraction.Conversations.Models;

public class ConversationState : Dictionary<string, string>
{
    public ConversationState()
    {
        
    }

    public ConversationState(List<StateKeyValue> pairs)
    {
        foreach (var pair in pairs)
        {
            this[pair.Key] = pair.Value;
        }
    }

    public List<StateKeyValue> ToKeyValueList()
    {
        return this.Select(x => new StateKeyValue(x.Key, x.Value)).ToList();
    }
}
