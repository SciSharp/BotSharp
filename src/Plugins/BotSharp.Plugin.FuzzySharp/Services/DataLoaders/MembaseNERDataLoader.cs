using BotSharp.Abstraction.Graph;
using BotSharp.Abstraction.Graph.Options;
using BotSharp.Abstraction.Infrastructures;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace BotSharp.Plugin.FuzzySharp.Services.DataLoaders;

/// <summary>
/// Loads NER vocabulary / synonyms from a graph database (e.g. Membase) per-call.
/// Required context parameter:
///   - "graphId": target graph identifier (non-empty string).
/// Vocabulary schemas are configured per-tenant under
/// <c>FuzzySharp:Membase:Tenants:&lt;alias&gt;:Schema</c> (committed in appsettings),
/// with the tenant's environment-specific <c>GraphId</c> supplied via user-secrets
/// or appsettings.{Environment}.json. The loader resolves the incoming graphId to a
/// tenant via the merged configuration. Each label yields
/// <c>MATCH (n:Label) RETURN n.Property AS text</c>. Synonyms still read the flat
/// (:Synonym {table, column, term, canonical_form}) schema.
/// Exposes InvalidateCacheAsync(graphId) so write paths can force a refresh.
/// </summary>
public class MembaseNERDataLoader : IEntityDataLoader
{
    private const string GraphIdKey = "graphId";
    private const string CacheKeyPrefix = "fuzzysharp:ner";
    private const int CacheMinutes = 60;
    private const string GraphDbProvider = "membase";

    private const string SynonymCypher =
        "MATCH (n:Synonym) RETURN n.table AS table, n.column AS column, n.term AS term, n.canonical_form AS canonical_form";

    private static readonly Regex IdentifierRegex = new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

    private readonly ILogger<MembaseNERDataLoader> _logger;
    private readonly IEnumerable<IGraphDb> _graphDbs;
    private readonly ICacheService _cache;
    private readonly FuzzySharpSettings _settings;

    public MembaseNERDataLoader(
        ILogger<MembaseNERDataLoader> logger,
        IEnumerable<IGraphDb> graphDbs,
        ICacheService cache,
        FuzzySharpSettings settings)
    {
        _logger = logger;
        _graphDbs = graphDbs;
        _cache = cache;
        _settings = settings;
    }

    public string Provider => "fuzzy-sharp-membase";

    private static string VocabKey(string graphId) => $"{CacheKeyPrefix}:vocab:{graphId}";
    private static string SynonymKey(string graphId) => $"{CacheKeyPrefix}:synonym:{graphId}";

    // The parameterless overloads don't make sense for a graph-backed loader.
    // Caller must supply a graphId via EntityDataLoadContext.
    public Task<Dictionary<string, HashSet<string>>> LoadVocabularyAsync()
        => Task.FromResult(new Dictionary<string, HashSet<string>>());

    public Task<Dictionary<string, (string DataSource, string CanonicalForm)>> LoadSynonymMappingAsync()
        => Task.FromResult(new Dictionary<string, (string DataSource, string CanonicalForm)>());

    public Task<Dictionary<string, HashSet<string>>> LoadVocabularyAsync(EntityDataLoadContext ctx)
    {
        if (!TryGetGraphId(ctx, out var graphId))
        {
            return Task.FromResult(new Dictionary<string, HashSet<string>>());
        }
        return LoadVocabularyByGraphIdAsync(graphId);
    }

    public Task<Dictionary<string, (string DataSource, string CanonicalForm)>> LoadSynonymMappingAsync(EntityDataLoadContext ctx)
    {
        if (!TryGetGraphId(ctx, out var graphId))
        {
            return Task.FromResult(new Dictionary<string, (string DataSource, string CanonicalForm)>());
        }
        return LoadSynonymMappingByGraphIdAsync(graphId);
    }

    private async Task<Dictionary<string, HashSet<string>>> LoadVocabularyByGraphIdAsync(string graphId)
    {
        var key = VocabKey(graphId);
        var cached = await _cache.GetAsync<Dictionary<string, HashSet<string>>>(key);
        if (cached != null) return cached;

        var result = new Dictionary<string, HashSet<string>>();

        var sources = _settings.Membase?.VocabularySources;
        if (sources == null || !sources.TryGetValue(graphId, out var labelMap) || labelMap == null || labelMap.Count == 0)
        {
            _logger.LogWarning($"Skip {Provider}: no vocabulary sources configured for graphId='{graphId}' under FuzzySharp:Membase:VocabularySources.");
            return result;
        }

        var graphDb = ResolveGraphDb();
        if (graphDb == null) return result;

        foreach (var (label, fields) in labelMap)
        {
            if (fields == null || fields.Length == 0) continue;

            if (!IdentifierRegex.IsMatch(label))
            {
                _logger.LogWarning($"Skip vocabulary label '{label}' in {Provider}: invalid identifier.");
                continue;
            }

            // Build aliased projections: n.prop0 AS f0, n.prop1 AS f1, ...
            // Carry SqlSource alongside so we can key the result dict by SQL "table.column".
            var validFields = new List<(string Alias, string GraphProperty, string SqlSource)>(fields.Length);
            for (var i = 0; i < fields.Length; i++)
            {
                var graphProperty = fields[i].GraphProperty;
                var sqlSource = fields[i].SqlSource;
                if (string.IsNullOrWhiteSpace(graphProperty) || !IdentifierRegex.IsMatch(graphProperty))
                {
                    _logger.LogWarning($"Skip vocabulary field '{label}.{graphProperty}' in {Provider}: invalid identifier.");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(sqlSource))
                {
                    _logger.LogWarning($"Skip vocabulary field '{label}.{graphProperty}' in {Provider}: empty SqlSource.");
                    continue;
                }
                validFields.Add(($"f{i}", graphProperty, sqlSource));
            }
            if (validFields.Count == 0) continue;

            var projection = string.Join(", ", validFields.Select(f => $"n.{f.GraphProperty} AS {f.Alias}"));
            var cypher = $"MATCH (n:{label}) RETURN {projection}";

            try
            {
                var queryResult = await graphDb.ExecuteQueryAsync(cypher, new GraphQueryExecuteOptions
                {
                    GraphId = graphId
                });

                foreach (var (alias, _, sqlSource) in validFields)
                {
                    if (!result.TryGetValue(sqlSource, out var set))
                    {
                        set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        result[sqlSource] = set;
                    }

                    foreach (var row in queryResult?.Values ?? [])
                    {
                        var text = row.TryGetValue(alias, out var tx) ? tx?.ToString() : null;
                        if (string.IsNullOrWhiteSpace(text)) continue;
                        set.Add(text!.Trim());
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading vocabulary label '{label}' from {Provider} graphId={graphId}");
            }
        }

        _logger.LogInformation($"Loaded vocabulary from {Provider} graphId={graphId}: {result.Sum(x => x.Value.Count)} terms across {result.Count} sources");
        await _cache.SetAsync(key, result, TimeSpan.FromMinutes(CacheMinutes));

        return result;
    }

    private async Task<Dictionary<string, (string DataSource, string CanonicalForm)>> LoadSynonymMappingByGraphIdAsync(string graphId)
    {
        var key = SynonymKey(graphId);
        var cached = await _cache.GetAsync<Dictionary<string, (string DataSource, string CanonicalForm)>>(key);
        if (cached != null) return cached;

        var result = new Dictionary<string, (string DataSource, string CanonicalForm)>();
        var graphDb = ResolveGraphDb();
        if (graphDb == null) return result;

        try
        {
            var queryResult = await graphDb.ExecuteQueryAsync(SynonymCypher, new GraphQueryExecuteOptions
            {
                GraphId = graphId
            });

            foreach (var row in queryResult?.Values ?? [])
            {
                var term = row.TryGetValue("term", out var t) ? t?.ToString() : null;
                var table = row.TryGetValue("table", out var tb) ? tb?.ToString() : null;
                var column = row.TryGetValue("column", out var co) ? co?.ToString() : null;
                var canonical = row.TryGetValue("canonical_form", out var c) ? c?.ToString() : null;
                if (string.IsNullOrWhiteSpace(term) || string.IsNullOrWhiteSpace(table) || string.IsNullOrWhiteSpace(column) || string.IsNullOrWhiteSpace(canonical)) continue;

                var dbPath = $"{table!.Trim()}.{column!.Trim()}";
                result[term!.Trim().ToLowerInvariant()] = (dbPath, canonical!);
            }

            _logger.LogInformation($"Loaded synonym mapping from {Provider} graphId={graphId}: {result.Count} terms");
            await _cache.SetAsync(key, result, TimeSpan.FromMinutes(CacheMinutes));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error loading synonym mapping from {Provider} graphId={graphId}");
        }

        return result;
    }

    public async Task InvalidateCacheAsync(string graphId)
    {
        if (string.IsNullOrWhiteSpace(graphId)) return;
        await _cache.RemoveAsync(VocabKey(graphId));
        await _cache.RemoveAsync(SynonymKey(graphId));
    }

    private IGraphDb? ResolveGraphDb()
    {
        var graphDb = _graphDbs.FirstOrDefault(x => string.Equals(x.Provider, GraphDbProvider, StringComparison.OrdinalIgnoreCase));
        if (graphDb == null)
        {
            _logger.LogWarning($"No IGraphDb registered with provider '{GraphDbProvider}'. Skip {Provider}.");
        }
        return graphDb;
    }

    private bool TryGetGraphId(EntityDataLoadContext ctx, out string graphId)
    {
        if (ctx.Parameters.TryGetValue(GraphIdKey, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            graphId = value;
            return true;
        }
        graphId = string.Empty;
        _logger.LogWarning($"Skip {Provider}: '{GraphIdKey}' not provided in EntityDataLoadContext.");
        return false;
    }
}
