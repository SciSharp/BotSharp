
namespace BotSharp.Abstraction.Knowledges.Models
{
    public class SearchPhrasesResult
    {
        public string Token { get; set; } = string.Empty;
        public List<string> DomainTypes { get; set; } = new();
        public string CanonicalForm { get; set; } = string.Empty;
        public double Confidence { get; set; }
    }
}