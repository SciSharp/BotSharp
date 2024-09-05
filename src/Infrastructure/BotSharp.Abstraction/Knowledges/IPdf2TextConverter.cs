namespace BotSharp.Abstraction.Knowledges
{
    public interface IPdf2TextConverter
    {
        public string Provider { get; }
        Task<string> ConvertPdfToText(string filePath, int? startPageNum, int? endPageNum);
    }
}