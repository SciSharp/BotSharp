using System.Text.Json.Serialization;

namespace BotSharp.Abstraction.Conversations.Models;

public class FunctionExecutionResult<T> where T : new()
{
    private readonly string _name;

    public FunctionExecutionResult(string name)
    {
        _name = name;
    }

    [JsonPropertyName("function_name")]
    public string Name => _name;

    [JsonPropertyName("execution_result")]
    public T Result { get; set; } = new T();
}
