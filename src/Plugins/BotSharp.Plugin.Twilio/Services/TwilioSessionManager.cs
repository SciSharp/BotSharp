using StackExchange.Redis;
using Task = System.Threading.Tasks.Task;

namespace BotSharp.Plugin.Twilio.Services
{
    public class TwilioSessionManager : ITwilioSessionManager
    {
        private readonly ConnectionMultiplexer _redis;

        public TwilioSessionManager(ConnectionMultiplexer redis)
        {
            _redis = redis;
        }

        public async Task<string> GetAssistantReplyAsync(string sessionId, int seqNum)
        {
            var db = _redis.GetDatabase();
            var key = $"{sessionId}:Assisist:{seqNum}";
            return await db.StringGetAsync(key);
        }

        public async Task<List<string>> RetrieveStagedCallerMessagesAsync(string sessionId, int seqNum)
        {
            var db = _redis.GetDatabase();
            var key = $"{sessionId}:Caller:{seqNum}";
            return (await db.ListRangeAsync(key))
                .Select(x => (string)x)
                .ToList();
        }

        public async Task SetAssistantReplyAsync(string sessionId, int seqNum, string message)
        {
            var db = _redis.GetDatabase();
            var key = $"{sessionId}:Assisist:{seqNum}";
            await db.StringSetAsync(key, message, TimeSpan.FromMinutes(5));
        }

        public async Task StageCallerMessageAsync(string sessionId, int seqNum, string message)
        {
            var db = _redis.GetDatabase();
            var key = $"{sessionId}:Caller:{seqNum}";
            await db.ListRightPushAsync(key, message);
            await db.KeyExpireAsync(key, DateTime.UtcNow.AddMinutes(10));
        }
    }
}
