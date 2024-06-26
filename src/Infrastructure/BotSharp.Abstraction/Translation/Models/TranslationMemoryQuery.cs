namespace BotSharp.Abstraction.Translation.Models;

public class TranslationMemoryQuery
{
    public string OriginalText { get; set; }
    public string HashText { get; set; }
    public string Language { get; set; }

    public TranslationMemoryQuery()
    {
        
    }

    public override string ToString()
    {
        return $"[Origin: {OriginalText}] translates to [{Language}]";
    }
}

public class TranslationMemoryInput : TranslationMemoryQuery
{
    public string TranslatedText { get; set; }

    public TranslationMemoryInput()
    {
        
    }

    public override string ToString()
    {
        return $"[Origin: {OriginalText}] -> [Translation: {TranslatedText}] (Language: {Language})";
    }
}

public class TranslationMemoryOutput: TranslationMemoryQuery
{
    public string TranslatedText { get; set; }

    public TranslationMemoryOutput()
    {
        
    }

    public override string ToString()
    {
        return $"[Origin: {OriginalText}] -> [Translation: {TranslatedText}] (Language: {Language})";
    }
}