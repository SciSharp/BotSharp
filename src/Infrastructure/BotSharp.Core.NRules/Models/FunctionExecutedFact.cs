namespace BotSharp.Core.NRules.Models
{
    public class FunctionExecutedFact
    {
        public string FunctionName { get; set; }
        public string Output { get; internal set; }
        public bool IsSuccess { get; internal set; }
    }
}