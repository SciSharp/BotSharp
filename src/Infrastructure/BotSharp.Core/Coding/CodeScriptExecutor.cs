namespace BotSharp.Core.Coding;

public class CodeScriptExecutor
{
    private readonly CodingSettings _settings;
    private readonly ILogger<CodeScriptExecutor> _logger;
    private readonly SemaphoreSlim _semLock = new(initialCount: 1, maxCount: 1);

    public CodeScriptExecutor(
        CodingSettings settings,
        ILogger<CodeScriptExecutor> logger)
    {
        _settings = settings;
        _logger = logger;

        var maxConcurrency = settings.CodeExecution?.MaxConcurrency > 0 ? settings.CodeExecution.MaxConcurrency : 1;
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
