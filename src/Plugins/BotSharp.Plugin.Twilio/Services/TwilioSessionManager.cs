using BotSharp.Plugin.Twilio.Models;
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

        public async Task<AssistantMessage> GetAssistantReplyAsync(string conversationId, int seqNum)
        {
            var db = _redis.GetDatabase();
            var key = $"{conversationId}:Assisist:{seqNum}";
            var jsonStr = await db.StringGetAsync(key);
            return jsonStr.IsNull ? null : JsonSerializer.Deserialize<AssistantMessage>(jsonStr);
        }

        public async Task<List<string>> RetrieveStagedCallerMessagesAsync(string conversationId, int seqNum)
        {
            var db = _redis.GetDatabase();
            var key = $"{conversationId}:Caller:{seqNum}";
            return (await db.ListRangeAsync(key)).Select(x => (string)x).ToList();
        }

        public async Task SetAssistantReplyAsync(string conversationId, int seqNum, AssistantMessage message)
        {
            var jsonStr = JsonSerializer.Serialize(message);
            var db = _redis.GetDatabase();
            var key = $"{conversationId}:Assisist:{seqNum}";
            await db.StringSetAsync(key, jsonStr, TimeSpan.FromMinutes(5));
        }

        public async Task StageCallerMessageAsync(string conversationId, int seqNum, string message)
        {
            var db = _redis.GetDatabase();
            var key = $"{conversationId}:Caller:{seqNum}";
            await db.ListRightPushAsync(key, message);
            await db.KeyExpireAsync(key, DateTime.UtcNow.AddMinutes(10));
        }

        public async Task SetReplyIndicationAsync(string conversationId, int seqNum, string indication)
        {
            var db = _redis.GetDatabase();
            var key = $"{conversationId}:Indication:{seqNum}";
            await db.StringSetAsync(key, indication, TimeSpan.FromMinutes(5));
        }

        public async Task<string> GetReplyIndicationAsync(string conversationId, int seqNum)
        {
            var db = _redis.GetDatabase();
            var key = $"{conversationId}:Indication:{seqNum}";
            return await db.StringGetAsync(key);
        }

        public async Task RemoveReplyIndicationAsync(string conversationId, int seqNum)
        {
            var db = _redis.GetDatabase();
            var key = $"{conversationId}:Indication:{seqNum}";
            await db.KeyDeleteAsync(key);
        }
    }
}
