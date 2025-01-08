namespace BotSharp.Abstraction.Browsing.Models;

public class BrowserActionResult
{
    public int ResponseStatusCode { get; set; }
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public string? StackTrace { get; set; }
    public string? Selector { get; set; }
    public string? Body { get; set; }
    /// <summary>
    /// Page open in new tab after button click
    /// </summary>
    public string? UrlAfterAction { get; set; }
    public bool IsHighlighted { get; set; }

    public override string ToString()
    {
        return $"{IsSuccess} - {Selector}";
    }
}
