using BotSharp.Abstraction.Utilities;
using BotSharp.Core.Infrastructures;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.IO;

namespace BotSharp.Plugin.FuzzySharp.Services.DataLoaders;

public class CsvNERDataLoader : INERDataLoader
{
    private readonly ILogger<CsvNERDataLoader> _logger;
    private readonly FuzzySharpSettings _settings;
    private readonly string _basePath;

    public CsvNERDataLoader(
        ILogger<CsvNERDataLoader> logger,
        FuzzySharpSettings settings)
    {
        _settings = settings;
        _logger = logger;
        _basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, settings.Data?.BaseDir ?? "data/tokens");
    }

    public string Provider => "fuzzy-sharp-csv";

#if !DEBUG
    [SharpCache(60)]
#endif
    public async Task<Dictionary<string, HashSet<string>>> LoadVocabularyAsync()
    {
        var vocabulary = new Dictionary<string, HashSet<string>>();

        // Load CSV files from the folder
        var folderName = _settings.Data?.Vocabulary?.Folder ?? string.Empty;
        var fileNames = _settings.Data?.Vocabulary?.FileNames?.Where(x => Path.GetFileName(x).EndsWith(".csv"));
        var csvFileDict = GetCsvFilesMetaData(folderName, fileNames);
        if (csvFileDict.IsNullOrEmpty())
        {
            return vocabulary;
        }

        // Load each CSV file
        foreach (var (source, filePath) in csvFileDict)
        {
            try
            {
                var terms = await LoadVocabularyFileAsync(filePath);
                vocabulary[source] = terms;
                _logger.LogInformation($"Loaded {terms.Count} terms for source '{source}' from {filePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading CSV file for source '{source}': {filePath}");
            }
        }

        return vocabulary;
    }

#if !DEBUG
    [SharpCache(60)]
#endif
    public async Task<Dictionary<string, (string DataSource, string CanonicalForm)>> LoadSynonymMappingAsync()
    {
        var result = new Dictionary<string, (string DataSource, string CanonicalForm)>();

        var folderName = _settings.Data?.Synonym?.Folder ?? string.Empty;
        var fileNames = _settings.Data?.Synonym?.FileNames?.Where(x => Path.GetFileName(x).EndsWith(".csv"));
        var csvFileDict = GetCsvFilesMetaData(folderName, fileNames);
        if (csvFileDict.IsNullOrEmpty())
        {
            return result;
        }

        // Load each CSV file
        foreach (var (source, filePath) in csvFileDict)
        {
            try
            {
                var mapping = await LoadSynonymFileAsync(filePath);
                foreach (var item in mapping)
                {
                    result[item.Key] = item.Value;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading CSV file for source '{source}': {filePath}");
            }
        }

        return result;
    }


    #region Private methods
    /// <summary>
    /// Load [csv file name] => file path
    /// </summary>
    /// <param name="folderName"></param>
    /// <returns></returns>
    private Dictionary<string, string> GetCsvFilesMetaData(string folderName, IEnumerable<string>? fileNames = null)
    {
        var csvFileDict = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(folderName))
        {
            return csvFileDict;
        }

        var searchFolder = Path.Combine(_basePath, folderName);
        if (!Directory.Exists(searchFolder))
        {
            _logger.LogWarning($"Folder does not exist: {searchFolder}");
            return csvFileDict;
        }

        IEnumerable<string> csvFiles = new List<string>();
        if (!fileNames.IsNullOrEmpty())
        {
            csvFiles = fileNames!.Select(x => Path.Combine(searchFolder, x));
        }
        else
        {
            csvFiles = Directory.GetFiles(searchFolder, "*.csv");
        }

        foreach (var file in csvFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            csvFileDict[fileName] = file;
        }

        _logger.LogInformation($"Loaded {csvFileDict.Count} CSV files from {searchFolder}");
        return csvFileDict;
    }

    /// <summary>
    /// Load the first column in the vocabulary file
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    private async Task<HashSet<string>> LoadVocabularyFileAsync(string filePath)
    {
        var terms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            _logger.LogWarning($"CSV file does not exist: {filePath}");
            return terms;
        }

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false  // No header in the CSV files
        });

        while (await csv.ReadAsync())
        {
            // Read the first column (assuming it contains the terms)
            var term = csv.GetField(0);
            if (!string.IsNullOrWhiteSpace(term))
            {
                terms.Add(term.Trim());
            }
        }

        _logger.LogInformation($"Loaded {terms.Count} terms from {Path.GetFileName(filePath)}");
        return terms;
    }


    private async Task<Dictionary<string, (string DataSource, string CanonicalForm)>> LoadSynonymFileAsync(string filePath)
    {
        var result = new Dictionary<string, (string DataSource, string CanonicalForm)>();

        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            _logger.LogWarning($"CSV file does not exist: {filePath}");
            return result;
        }

        try
        {
            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, CreateCsvConfig());

            await csv.ReadAsync();
            csv.ReadHeader();

            if (!HasRequiredColumns(csv))
            {
                _logger.LogWarning("Synonym mapping file missing required columns: {FilePath}", filePath);
                return result;
            }

            while (await csv.ReadAsync())
            {
                var term = csv.GetField<string>("term") ?? string.Empty;
                var dataSource = csv.GetField<string>("dbPath") ?? string.Empty;
                var canonicalForm = csv.GetField<string>("canonical_form") ?? string.Empty;

                if (term.Length == 0 || dataSource.Length == 0 || canonicalForm.Length == 0)
                {
                    _logger.LogWarning(
                        "Missing column(s) in CSV at row {Row}: term={Term}, dataSource={dataSource}, canonical_form={CanonicalForm}",
                        csv.Parser.RawRow,
                        term ?? "<null>",
                        dataSource ?? "<null>",
                        canonicalForm ?? "<null>");
                    continue;
                }

                var key = term.ToLowerInvariant();
                result[key] = (dataSource, canonicalForm);
            }

            _logger.LogInformation("Loaded synonym mapping from {FilePath}: {Count} terms", filePath, result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading synonym mapping file: {FilePath}", filePath);
            return result;
        }
    }


    private static CsvConfiguration CreateCsvConfig()
    {
        return new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            DetectColumnCountChanges = true,
            MissingFieldFound = null
        };
    }

    private static bool HasRequiredColumns(CsvReader csv)
    {
        return csv.HeaderRecord is { Length: > 0 } headers
               && headers.Contains("term")
               && headers.Contains("dbPath")
               && headers.Contains("canonical_form");
    }
    #endregion
}
