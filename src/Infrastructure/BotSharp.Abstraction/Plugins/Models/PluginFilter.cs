namespace BotSharp.Abstraction.Plugins.Models
{
    public class PluginFilter
    {
        public Pagination Pager { get; set; } = new Pagination();
        public IEnumerable<string>? Names { get; set; }
        public string? SimilarName { get; set; }
    }
}
