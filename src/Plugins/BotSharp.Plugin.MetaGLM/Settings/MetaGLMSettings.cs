namespace BotSharp.Plugin.MetaGLM.Settings;

public  class MetaGLMSettings
{
    /// <summary>
    /// In the new mechanism, the API Key issued by the platform contains both “user identifier id” and “signature key secret” in the format of {id}.{secret}.
    /// </summary>
    public string ApiKey { get; set; }

    public string BaseAddress { get; set; } = "https://open.bigmodel.cn/api/paas/v4/";

    public string ModelId { get; set; }

    public double Temperature { get; set; }

    public double TopP {  get; set; }
}
