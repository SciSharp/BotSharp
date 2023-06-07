namespace BotSharp.Abstraction.TextGeneratives;

public class TextToken
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public float Logprob { get;set; }
    public bool Special { get; set; }
}
