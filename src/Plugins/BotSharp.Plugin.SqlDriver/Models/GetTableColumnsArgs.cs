using System.Text.Json.Serialization;

namespace BotSharp.Plugin.SqlDriver.Models;

public class GetTableColumnsArgs
{
    [JsonPropertyName("table")]
    public string Table {  get; set; }
}
