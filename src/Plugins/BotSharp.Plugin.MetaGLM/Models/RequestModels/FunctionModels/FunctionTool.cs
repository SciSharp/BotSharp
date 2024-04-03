namespace BotSharp.Plugin.MetaGLM.Models.RequestModels.FunctionModels;

public class FunctionTool
{
    public string type { get; set; } = "function";
    public Dictionary<string, object> function { get; set; } = new();

    public FunctionTool SetName(string name)
    {
        this.function["name"] = name;
        return this;
    }

    public FunctionTool SetDescription(string desc)
    {
        this.function["description"] = desc;
        return this;
    }

    public FunctionTool SetParameters(FunctionParameters param)
    {
        this.function["parameters"] = param;
        return this;
    }
}