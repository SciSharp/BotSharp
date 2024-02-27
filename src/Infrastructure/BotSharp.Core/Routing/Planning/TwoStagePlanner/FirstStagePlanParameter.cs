using System.Text.Json.Serialization;

public class FirstStagePlanParameter
{
    [JsonPropertyName("input_args")]
    public JsonDocument[] Parameters { get; set; } = new JsonDocument[0];

    [JsonPropertyName("output_results")]
    public string[] Results { get; set; } = new string[0];

    public override string ToString()
    {
        return $"INPUTS:\r\n{JsonSerializer.Serialize(Parameters)}\r\n\r\nOUTPUTS:\r\n{JsonSerializer.Serialize(Results)}";
    }
}