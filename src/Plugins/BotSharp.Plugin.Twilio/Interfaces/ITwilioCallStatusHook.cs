using Task = System.Threading.Tasks.Task;

namespace BotSharp.Plugin.Twilio.Interfaces;

public interface ITwilioCallStatusHook
{
    Task OnVoicemailLeft(string conversationId);
}
