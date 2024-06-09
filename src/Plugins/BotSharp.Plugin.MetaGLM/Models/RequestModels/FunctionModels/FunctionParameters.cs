namespace BotSharp.Plugin.MetaGLM.Models.RequestModels.FunctionModels;

public class FunctionParameters
{
    public string type { get; set; }
    public Dictionary<string, FunctionParameterDescriptor> properties { get; }
    public string[] required { get; set; }

    public FunctionParameters()
    {
        this.type = "object";
        this.properties = new Dictionary<string, FunctionParameterDescriptor>();
    }

    public FunctionParameters AddParameter(string name, ParameterType type, string description)
    {
        properties[name] = new FunctionParameterDescriptor(type, description);
        return this;
    }

    public FunctionParameters SetRequiredParameter(string[] required)
    {
        this.required = required;
        return this;
    }
}