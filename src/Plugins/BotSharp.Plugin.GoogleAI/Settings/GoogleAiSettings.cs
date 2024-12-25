namespace BotSharp.Plugin.GoogleAi.Settings;

public class GoogleAiSettings
{
    public PaLMSetting PaLM {  get; set; }

    public GeminiSetting Gemini { get; set; }
}

public class PaLMSetting
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; }
}


public class GeminiSetting
{
    public string ApiKey { get; set; }
    public bool UseGoogleSearch { get; set; }
    public bool UseGrounding { get; set; }
}
