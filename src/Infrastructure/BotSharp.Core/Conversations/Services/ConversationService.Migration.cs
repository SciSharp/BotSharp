using NetTopologySuite.Algorithm;
using System.Diagnostics;

namespace BotSharp.Core.Conversations.Services;

public partial class ConversationService
{
    public async Task<bool> MigrateLatestStates(int batchSize = 100, int errorLimit = 10)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var isSuccess = true;
        var errorCount = 0;
        var batchNum = 0;
        var info = string.Empty;
        var error = string.Empty;

#if DEBUG
        Console.WriteLine($"\r\n#Start migrating Conversation Latest States...\r\n");
#else
        _logger.LogInformation($"#Start migrating Conversation Latest States...");
#endif
        var sw = Stopwatch.StartNew();

        var convIds = db.GetConversationsToMigrate(batchSize);
        
        while (!convIds.IsNullOrEmpty())
        {
            batchNum++;
            var innerSw = Stopwatch.StartNew();
#if DEBUG
            Console.WriteLine($"\r\n#Start migrating Conversation Latest States (batch number: {batchNum})\r\n");
#else
            _logger.LogInformation($"#Start migrating Conversation Latest States (batch number: {batchNum})");
#endif

            for (int i = 0; i < convIds.Count; i++)
            {
                var convId = convIds.ElementAt(i);
                try
                {
                    var done = db.MigrateConvsersationLatestStates(convId);
                    info = $"Conversation {convId} latest states have been migrated ({i + 1}/{convIds.Count})!";
#if DEBUG
                    Console.WriteLine($"\r\n{info}\r\n");
#else
                    _logger.LogInformation($"{info}");
#endif
                }
                catch (Exception ex)
                {
                    errorCount++;
                    error = $"Conversation {convId} latest states fail to be migrated! ({i + 1}/{convIds.Count})\r\n{ex.Message}\r\n{ex.InnerException}";
#if DEBUG
                    Console.WriteLine($"\r\n{error}\r\n");
#else
                    _logger.LogError($"{error}");
#endif
                }
            }

            if (errorCount >= errorLimit)
            {
                error = $"\r\nErrors exceed limit => stop the migration!\r\n";
#if DEBUG
                Console.WriteLine($"{error}");
#else
                _logger.LogError($"{error}");
#endif
                innerSw.Stop();
                isSuccess = false;
                break;
            }

            innerSw.Stop();
            info = $"#Done migrating Conversation Latest States (batch number: {batchNum}) " +
                $"(Total time: {innerSw.Elapsed.Hours} hrs, {innerSw.Elapsed.Minutes} mins, {innerSw.Elapsed.Seconds} seconds)";
#if DEBUG
            Console.WriteLine($"\r\n{info}\r\n");
#else
            _logger.LogInformation($"{info}");
#endif

            await Task.Delay(100);
            convIds = db.GetConversationsToMigrate(batchSize);
        }

        sw.Stop();
        info = $"#Done with migrating Conversation Latest States! " +
            $"(Total time: {sw.Elapsed.Days} days, {sw.Elapsed.Hours} hrs, {sw.Elapsed.Minutes} mins, {sw.Elapsed.Seconds} seconds)";
#if DEBUG
        Console.WriteLine($"\r\n{info}\r\n");
#else
        _logger.LogInformation($"{info}");
#endif

        return isSuccess;
    }
}
