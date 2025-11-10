namespace BotSharp.Core.Coding;

public class CodeScriptExecutor
{
    private const int DEFAULT_MAX_CONCURRENCY = 1;

    private readonly CodingSettings _settings;
    private readonly ILogger<CodeScriptExecutor> _logger;
    private readonly SemaphoreSlim _semLock = new(initialCount: DEFAULT_MAX_CONCURRENCY, maxCount: DEFAULT_MAX_CONCURRENCY);

    public CodeScriptExecutor(
        CodingSettings settings,
        ILogger<CodeScriptExecutor> logger)
    {
        _settings = settings;
        _logger = logger;

        var maxConcurrency = settings.CodeExecution?.MaxConcurrency > 0 ? settings.CodeExecution.MaxConcurrency : DEFAULT_MAX_CONCURRENCY;
        _semLock = new(initialCount: maxConcurrency, maxCount: maxConcurrency);
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> func, CancellationToken cancellationToken = default)
    {
        await _semLock.WaitAsync(cancellationToken);

        try
        {
            return await func();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error in {nameof(CodeScriptExecutor)}.");
            return default(T);
        }
        finally
        {
            _semLock.Release();
        }
    }
}
