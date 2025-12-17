using Polly;
using Polly.Retry;
using StackExchange.Redis;

namespace BotSharp.Core.Infrastructures;

public static class PollyExtensions
{
    public static AsyncRetryPolicy CreateRedisRetryPolicy(
        ILogger logger,
        int retryCount = 3,
        Func<int, TimeSpan>? sleepDurationProvider = null)
    {
        sleepDurationProvider ??= retryAttempt => TimeSpan.FromSeconds(Math.Pow(3, retryAttempt)); 

        return Policy
            .Handle<RedisTimeoutException>()
            .Or<RedisConnectionException>()
            .WaitAndRetryAsync(
                retryCount: retryCount,
                sleepDurationProvider: sleepDurationProvider,
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var operation = context.ContainsKey("operation") ? context["operation"]?.ToString() : "Redis operation";
                    logger.LogWarning("{Operation} retry {RetryCount}/{MaxRetries} after {Delay}s. Context: {Context}. Exception: {Exception}",
                        operation, retryCount, retryCount, timespan.TotalSeconds,
                        context.GetContextStr(),
                        outcome.Message);
                });
    }

    public static async Task<T> RetryAsync<T>(
        this AsyncRetryPolicy policy,
        Func<Task<T>> operation,
        string operationName,
        ILogger logger,
        T? defaultValue = default,
        Context? context = null)
    {
        var ctx = context ?? [];
        ctx["operation"] = operationName;
        try
        {
            return await policy.ExecuteAsync(async (ctx) => await operation(), context);
        }
        catch (Exception ex)
        {
            logger.LogError(
                "Failed {Operation} after retries. Context: {Context}. Exception: {Exception}",
                operationName, ctx.GetContextStr(), ex.Message);

            return defaultValue ?? default(T)!;
        }
    }

    public static async Task<bool> RetryAsync(
        this AsyncRetryPolicy policy,
        Func<Task> operation,
        string operationName,
        ILogger logger,
        Context? context = null)
    {
        var ctx = context ?? new Context();
        ctx["operation"] = operationName;

        try
        {
            await policy.ExecuteAsync(async (c) => await operation(), ctx);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(
                "Failed {Operation} after retries. Context: {Context}. Exception: {Exception}",
                operationName, ctx.GetContextStr(), ex.Message);
            return false;
        }
    }

    private static string GetContextStr(this Context ctx)
    {
        return ctx != null ? string.Join(", ", ctx.Keys.Select(k => $"{k}={ctx[k]}")): string.Empty;
    }
}