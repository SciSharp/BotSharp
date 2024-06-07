namespace BotSharp.Plugin.MetaGLM.Models.ResponseModels.ToolModels;

public class ToolCallItem
{
    public string id { get; set; }
    public FunctionDescriptor function { get; set; }
    public int index { get; set; }
    public string type { get; set; }
}