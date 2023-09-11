namespace BotSharp.Abstraction.Conversations.Models;

public class StateKeyValue
{
    public string Key { get; set; }
    public string Value { get; set; }

    public StateKeyValue()
    {

    }

    public StateKeyValue(string key, string value)
    {
        Key = key;
        Value = value;
    }
}
