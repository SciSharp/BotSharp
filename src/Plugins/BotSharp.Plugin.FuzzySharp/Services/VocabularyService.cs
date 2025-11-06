using BotSharp.Abstraction.FuzzSharp;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.IO;

namespace BotSharp.Plugin.FuzzySharp.Services
{
    public class VocabularyService : IVocabularyService
    {
        private readonly ILogger<VocabularyService> _logger;

        public VocabularyService(ILogger<VocabularyService> logger)
        {
            _logger = logger;
        }

        public async Task<Dictionary<string, HashSet<string>>> LoadVocabularyAsync(string? foldername)
        {
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
            foreach (var (domainType, filePath) in csvFileDict)
            {
                try
                {
                    var terms = await LoadCsvFileAsync(filePath);
                    vocabulary[domainType] = terms;
                    _logger.LogInformation($"Loaded {terms.Count} terms for domain type '{domainType}' from {filePath}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error loading CSV file for domain type '{domainType}': {filePath}");
                }
            }

            return vocabulary;
        }

        public async Task<Dictionary<string, (string DbPath, string CanonicalForm)>> LoadDomainTermMappingAsync(string? filename)
        {
            var result = new Dictionary<string, (string DbPath, string CanonicalForm)>();
            if (string.IsNullOrWhiteSpace(filename))
            {
                return result;
            }

            var searchFolder = Path.Combine(AppContext.BaseDirectory, "data", "plugins", "fuzzySharp");
            var filePath = Path.Combine(searchFolder, filename);

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
                    _logger.LogWarning("Domain term mapping file missing required columns: {FilePath}", filePath);
                    return result;
                }

                while (await csv.ReadAsync())
                {
                    var term = csv.GetField<string>("term") ?? string.Empty;
                    var dbPath = csv.GetField<string>("dbPath") ?? string.Empty;
                    var canonicalForm = csv.GetField<string>("canonical_form") ?? string.Empty;

                    if (term.Length == 0 || dbPath.Length == 0 || canonicalForm.Length == 0)
                    {
                        _logger.LogWarning(
                            "Missing column(s) in CSV at row {Row}: term={Term}, dbPath={DbPath}, canonical_form={CanonicalForm}",
                            csv.Parser.RawRow,
                            term ?? "<null>",
                            dbPath ?? "<null>",
                            canonicalForm ?? "<null>");
                        continue;
                    }

                    var key = term.ToLowerInvariant();
                    result[key] = (dbPath, canonicalForm);
                }

                _logger.LogInformation("Loaded domain term mapping from {FilePath}: {Count} terms", filePath, result.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading domain term mapping file: {FilePath}", filePath);
            }

            return result;
        }

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

        private async Task<Dictionary<string, string>> LoadCsvFilesFromFolderAsync(string folderName)
        {
            var csvFileDict = new Dictionary<string, string>();
            var searchFolder = Path.Combine(AppContext.BaseDirectory, "data", "plugins", "fuzzySharp", folderName);
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
    }
}
