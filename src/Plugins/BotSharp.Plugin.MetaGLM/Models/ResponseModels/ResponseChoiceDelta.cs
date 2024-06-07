namespace BotSharp.Plugin.MetaGLM.Models.ResponseModels;

public class ResponseChoiceDelta
{
    public string role { get; set; }
    public string content { get; set; }
    public ToolCallItem[] tool_calls { get; set; }
}