namespace BotSharp.Plugin.GoogleAi.Settings;

public class GoogleAiSettings
{
    public PaLMSetting PaLM {  get; set; } = new();

    public GeminiSetting Gemini { get; set; } = new();
}

public class PaLMSetting
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}


public class GeminiSetting
{
    public string ApiKey { get; set; } = string.Empty;
    public bool UseGoogleSearch { get; set; }
    public bool UseGrounding { get; set; }
}
