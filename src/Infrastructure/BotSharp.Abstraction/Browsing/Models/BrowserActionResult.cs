namespace BotSharp.Abstraction.Browsing.Models;

public class BrowserActionResult
{
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; }
    public string StackTrace { get; set; }
    public string Selector { get; set; }
    public string Body { get; set; }
}
