namespace BotSharp.Abstraction.Tokenizers.Models;

public class TokenizeOptions
{
    [JsonPropertyName("max_ngram")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxNgram { get; set; }


}
