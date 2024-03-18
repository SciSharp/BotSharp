using BotSharp.Abstraction.Messaging.JsonConverters;
using System.Text.Json;

namespace BotSharp.Abstraction.Options;

public class BotSharpOptions
{
    private readonly static JsonSerializerOptions defaultJsonOptions = new JsonSerializerOptions()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        AllowTrailingCommas = true,
        WriteIndented = true
    };

    private JsonSerializerOptions _jsonSerializerOptions;

    public JsonSerializerOptions JsonSerializerOptions
    { 
        get
        {            
            return _jsonSerializerOptions ?? defaultJsonOptions;
        }
        set
        {
            if (value == null)
            {
                _jsonSerializerOptions = defaultJsonOptions;
            }
            else
            {
                _jsonSerializerOptions = value;
            }
        }
    }


    public BotSharpOptions()
    {
        
    }
}
