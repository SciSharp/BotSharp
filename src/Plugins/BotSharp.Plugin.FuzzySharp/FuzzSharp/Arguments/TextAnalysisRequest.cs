using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BotSharp.Abstraction.FuzzSharp.Arguments;

public class TextAnalysisRequest
{
    /// <summary>
    /// Text to analyze
    /// </summary>
    [Required]
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Folder path containing CSV files (will read all .csv files from the folder or its 'output' subfolder)
    /// </summary>
    [JsonPropertyName("vocabulary_folder_name")]
    public string? VocabularyFolderName { get; set; }

    /// <summary>
    /// Domain term mapping CSV file
    /// </summary>
    [JsonPropertyName("domain_term_mapping_file")]
    public string? DomainTermMappingFile { get; set; }

    /// <summary>
    /// Min score for suggestions (0.0-1.0)
    /// </summary>
    [JsonPropertyName("cutoff")]
    [Range(0.0, 1.0)]
    public double Cutoff { get; set; } = 0.80;

    /// <summary>
    /// Max candidates per domain (1-20)
    /// </summary>
    [JsonPropertyName("topk")]
    [Range(1, 20)]
    public int TopK { get; set; } = 5;

    /// <summary>
    /// Max n-gram size (1-10)
    /// </summary>
    [JsonPropertyName("max_ngram")]
    [Range(1, 10)]
    public int MaxNgram { get; set; } = 5;

    /// <summary>
    /// Include tokens field in response (default: False)
    /// </summary>
    [JsonPropertyName("include_tokens")]
    public bool IncludeTokens { get; set; } = false;
}