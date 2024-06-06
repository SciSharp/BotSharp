namespace BotSharp.Plugin.MetaGLM.Models.RequestModels.FunctionModels;

public enum ParameterType
{
    String,
    Integer,
}

public class FunctionParameterDescriptor
{
    public string type { get; set; }
    public string description { get; set; }

    private static string ToTypeString(ParameterType type)
    {
        return type switch
        {
            ParameterType.String => "string",
            ParameterType.Integer => "int",
            _ => null
        };
    }

    public FunctionParameterDescriptor(ParameterType type, string description)
    {
        this.type = ToTypeString(type);
        this.description = description;
    }
}