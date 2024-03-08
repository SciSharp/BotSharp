namespace BotSharp.Abstraction.Browsing.Models;

public class ElementActionArgs
{
    private string _action;
    public string Action => _action;

    public ElementActionArgs(string action)
    {
        _action = action;
    }
}
