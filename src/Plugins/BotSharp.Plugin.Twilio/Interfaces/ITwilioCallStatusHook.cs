using BotSharp.Plugin.Twilio.Models;
using Task = System.Threading.Tasks.Task;

namespace BotSharp.Plugin.Twilio.Interfaces;

public interface ITwilioCallStatusHook
{
    Task OnVoicemailLeft(ConversationalVoiceRequest request);
    Task OnUserDisconnected(ConversationalVoiceRequest request);
    Task OnRecordingCompleted(ConversationalVoiceRequest request);
    Task OnVoicemailStarting(ConversationalVoiceRequest request);
}
