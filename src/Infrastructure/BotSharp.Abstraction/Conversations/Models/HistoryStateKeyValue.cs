namespace BotSharp.Abstraction.Conversations.Models;

public class HistoryStateKeyValue
{
    public string Key { get; set; }
    public List<HistoryStateValue> Values { get; set; } = new List<HistoryStateValue>();

    public HistoryStateKeyValue()
    {

    }

    public HistoryStateKeyValue(string key, List<HistoryStateValue> values)
    {
        Key = key;
        Values = values;
    }
}

public class HistoryStateValue
{
    public string Data { get; set; }
    public DateTime UpdateTime { get; set; }

    public HistoryStateValue()
    {

    }
}