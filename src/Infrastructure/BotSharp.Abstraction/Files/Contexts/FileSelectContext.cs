namespace BotSharp.Abstraction.Files.Contexts;

public class FileSelectContext
{
    [JsonPropertyName("selected_files")]
    public List<FileSelectItem>? SelectedFiles { get; set; }
}

public class FileSelectItem
{
    [JsonPropertyName("message_id")]
    public string MessageId { get; set; }

    [JsonPropertyName("file_index")]
    public string FileIndex { get; set; }

    [JsonPropertyName("file_source")]
    public string FileSource { get; set; }
}