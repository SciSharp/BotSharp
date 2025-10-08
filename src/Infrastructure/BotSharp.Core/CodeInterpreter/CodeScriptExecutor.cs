using BotSharp.Abstraction.CodeInterpreter.Models;

namespace BotSharp.Core.CodeInterpreter;

public class CodeScriptExecutor
{
    private readonly ILogger<CodeScriptExecutor> _logger;
    private readonly SemaphoreSlim _semLock = new(initialCount: 1, maxCount: 1);

    public CodeScriptExecutor(
        ILogger<CodeScriptExecutor> logger)
    {
        _logger = logger;
    }

    public async Task<CodeInterpretResult> Execute(Func<Task<CodeInterpretResult>> func, CancellationToken cancellationToken = default)
    {
        await _semLock.WaitAsync(cancellationToken);

        try
        {
            return await func();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error in {nameof(CodeScriptExecutor)}.");
            return new CodeInterpretResult
            {
                Success = false,
                ErrorMsg = ex.Message
            };
        }
        finally
        {
            _semLock.Release();
        }
    }
}
