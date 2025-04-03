namespace BotSharp.Abstraction.ComputerUse.Models;

public class ComputerUseArgs
{
    /// <summary>
    /// Number of multi-screens
    /// </summary>
    public int DisplayId { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public string Text { get; set; } = string.Empty;
    public MouseButton MouseButton { get; set; }
    public KeyCode KeyCode { get; set; }
}
