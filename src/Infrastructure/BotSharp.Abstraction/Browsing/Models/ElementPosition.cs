namespace BotSharp.Abstraction.Browsing.Models;

public class ElementPosition
{
    public float X { get; set; } = default!;

    public float Y { get; set; } = default!;

    public override string ToString()
    {
        return $"[{X}, {Y}]";
    }
}
