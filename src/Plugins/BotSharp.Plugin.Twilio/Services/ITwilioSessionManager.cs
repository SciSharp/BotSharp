using BotSharp.Plugin.Twilio.Models;
using Task = System.Threading.Tasks.Task;

namespace BotSharp.Plugin.Twilio.Services
{
    public interface ITwilioSessionManager
    {
        Task SetAssistantReplyAsync(string conversationId, int seqNum, AssistantMessage message);
        Task<AssistantMessage> GetAssistantReplyAsync(string conversationId, int seqNum);
        Task StageCallerMessageAsync(string conversationId, int seqNum, string message);
        Task<List<string>> RetrieveStagedCallerMessagesAsync(string conversationId, int seqNum);
        Task SetReplyIndicationAsync(string conversationId, int seqNum, string indication);
        Task<string> GetReplyIndicationAsync(string conversationId, int seqNum);
    }
}
