using BotSharp.Abstraction.Browsing.Enums;

namespace BotSharp.Abstraction.Browsing.Models;

public class ElementActionArgs
{
    private BroswerActionEnum _action;
    public BroswerActionEnum Action => _action;

    private string _content;
    public string Content => _content;

    public ElementActionArgs(BroswerActionEnum action)
    {
        _action = action;
    }

    public ElementActionArgs(BroswerActionEnum action, string content)
    {
        _action = action;
        _content = content;
    }
}
