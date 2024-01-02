namespace BotSharp.Abstraction.Conversations.Models;

public class StateKeyValue
{
    public string Key { get; set; }
    public List<StateValue> Values { get; set; } = new List<StateValue>();

    public StateKeyValue()
    {

    }

    public StateKeyValue(string key, List<StateValue> values)
    {
        Key = key;
        Values = values;
    }
}

public class StateValue
{
    public string Data { get; set; }
    public DateTime UpdateTime { get; set; }

    public StateValue()
    {

    }
}