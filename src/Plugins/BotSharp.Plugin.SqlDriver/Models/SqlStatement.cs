using System.Text.Json.Serialization;

namespace BotSharp.Plugin.SqlDriver.Models;

public class SqlStatement
{
    [JsonPropertyName("sql_statement")]
    public string Statement { get; set; } = null!;

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = null!;

    [JsonPropertyName("table")]
    public string Table { get; set; } = null!;

    [JsonPropertyName("parameters")]
    public SqlParameter[] Parameters { get; set; } = [];

    [JsonPropertyName("return_field")]
    public SqlReturn Return { get; set; } = new SqlReturn();

    [JsonPropertyName("generated_without_table_definition")]
    public bool GeneratedWithoutTableDefinition { get; set; }

    public override string ToString()
    {
        return $"{Statement}\t {string.Join(", ", Parameters.Select(x => x.Name + ": " + x.Value))}";
    }
}
