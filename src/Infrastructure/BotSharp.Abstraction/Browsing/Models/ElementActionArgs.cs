using BotSharp.Abstraction.Browsing.Enums;

namespace BotSharp.Abstraction.Browsing.Models;

public class ElementActionArgs
{
    private BroswerActionEnum _action;
    public BroswerActionEnum Action => _action;

    private string? _content;
    public string? Content => _content;

    private ElementPosition? _position;
    public ElementPosition? Position => _position;

    public ElementActionArgs(BroswerActionEnum action, ElementPosition? position = null)
    {
        _action = action;
        _position = position;
    }

    public ElementActionArgs(BroswerActionEnum action, string content)
    {
        _action = action;
        _content = content;
    }
}
