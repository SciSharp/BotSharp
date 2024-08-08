using Task = System.Threading.Tasks.Task;

namespace BotSharp.Plugin.Twilio.Services
{
    public interface ITwilioSessionManager
    {
        Task SetAssistantReplyAsync(string sessionId, int seqNum, string message);
        Task<string> GetAssistantReplyAsync(string sessionId, int seqNum);
        Task StageCallerMessageAsync(string sessionId, int seqNum, string message);
        Task<List<string>> RetrieveStagedCallerMessagesAsync(string sessionId, int seqNum);
    }
}
