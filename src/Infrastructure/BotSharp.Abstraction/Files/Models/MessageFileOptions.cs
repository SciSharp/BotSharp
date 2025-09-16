namespace BotSharp.Abstraction.Files.Models;

public class MessageFileOptions
{
    /// <summary>
    /// File sources: user, bot
    /// </summary>
    public IEnumerable<string>? Sources { get; set; }

    /// <summary>
    /// File content types
    /// </summary>
    public IEnumerable<string>? ContentTypes { get; set; }
}
