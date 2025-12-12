using BotSharp.Core.Infrastructures;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.IO;

namespace BotSharp.Plugin.FuzzySharp.Services.DataLoaders;

public class CsvTokenDataLoader : ITokenDataLoader
{
    private readonly ILogger<CsvTokenDataLoader> _logger;
    private readonly FuzzySharpSettings _settings;
    private readonly string _basePath;

    public CsvTokenDataLoader(
        ILogger<CsvTokenDataLoader> logger,
        FuzzySharpSettings settings)
    {
        _settings = settings;
        _logger = logger;
        _basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, settings.Data?.BaseDir ?? "data/tokens/fuzzy-sharp");
    }

    public string Provider => "fuzzy-sharp-csv";

#if DEBUG
    [SharpCache(60)]
#endif
    public async Task<Dictionary<string, HashSet<string>>> LoadVocabularyAsync()
    {
        var foldername = _settings.Data?.VocabularyFolder;
        var vocabulary = new Dictionary<string, HashSet<string>>();

        if (string.IsNullOrEmpty(foldername))
        {
            return vocabulary;
        }

        // Load CSV files from the folder
        var csvFileDict = await LoadCsvFilesFromFolderAsync(foldername);
        if (csvFileDict.Count == 0)
        {
            return vocabulary;
        }

        // Load each CSV file
        foreach (var (source, filePath) in csvFileDict)
        {
            try
            {
                var terms = await LoadCsvFileAsync(filePath);
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

#if DEBUG
    [SharpCache(60)]
#endif
    public async Task<Dictionary<string, (string DataSource, string CanonicalForm)>> LoadSynonymMappingAsync()
    {
        var filename = _settings.Data?.VocabularyFolder;
        var result = new Dictionary<string, (string DataSource, string CanonicalForm)>();
        if (string.IsNullOrWhiteSpace(filename))
        {
            return result;
        }

        var filePath = Path.Combine(_basePath, filename);
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading synonym mapping file: {FilePath}", filePath);
        }

        return result;
    }


    #region Private methods
    /// <summary>
    /// Load [csv file name] => file path
    /// </summary>
    /// <param name="folderName"></param>
    /// <returns></returns>
    private async Task<Dictionary<string, string>> LoadCsvFilesFromFolderAsync(string folderName)
    {
        var csvFileDict = new Dictionary<string, string>();
        var searchFolder = Path.Combine(_basePath, folderName);
        if (!Directory.Exists(searchFolder))
        {
            _logger.LogWarning($"Folder does not exist: {searchFolder}");
            return csvFileDict;
        }

        var csvFiles = Directory.GetFiles(searchFolder, "*.csv");
        foreach (var file in csvFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            csvFileDict[fileName] = file;
        }

        _logger.LogInformation($"Loaded {csvFileDict.Count} CSV files from {searchFolder}");
        return await Task.FromResult(csvFileDict);
    }

    /// <summary>
    /// Load the first column in the csv file
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    private async Task<HashSet<string>> LoadCsvFileAsync(string filePath)
    {
        var terms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!File.Exists(filePath))
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
