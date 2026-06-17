namespace BotSharp.Abstraction.Entity.Models;

/// <summary>
/// Loader-facing context carrying free-form parameters from the caller
/// (e.g. via <see cref="EntityAnalysisOptions.LoaderParameters"/>).
/// Each <see cref="IEntityDataLoader"/> implementation defines which keys it
/// recognizes (document them on the concrete loader's XML doc).
/// </summary>
public class EntityDataLoadContext
{
    /// <summary>
    /// Case-insensitive key/value bag (e.g. "graphId", "tenantId").
    /// </summary>
    public IDictionary<string, string> Parameters { get; init; }
        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
