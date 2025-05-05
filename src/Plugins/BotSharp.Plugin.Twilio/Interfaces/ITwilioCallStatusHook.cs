using BotSharp.Plugin.Twilio.Models;
using Task = System.Threading.Tasks.Task;

namespace BotSharp.Plugin.Twilio.Interfaces;

public interface ITwilioCallStatusHook
{
    bool IsMatch(ConversationalVoiceRequest request) => true;
    Task OnVoicemailLeft(ConversationalVoiceRequest request) => Task.CompletedTask;
    Task OnUserDisconnected(ConversationalVoiceRequest request) => Task.CompletedTask;
    Task OnRecordingCompleted(ConversationalVoiceRequest request) => Task.CompletedTask;
    Task OnVoicemailStarting(ConversationalVoiceRequest request)=> Task.CompletedTask;

    /// <summary>
    /// 1. The recipient's phone line is already engaged.
    /// 2. Some users block unknown or spam calls, which may cause a "busy" status.
    /// 3. Some carriers explicitly return a busy signal instead of routing the call to voicemail.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    Task OnCallBusyStatus(ConversationalVoiceRequest request)=> Task.CompletedTask;

    Task OnCallNoAnswerStatus(ConversationalVoiceRequest request) => Task.CompletedTask;

    Task OnCallCanceledStatus(ConversationalVoiceRequest request)=> Task.CompletedTask;

    Task OnCallFailedStatus(ConversationalVoiceRequest request)=> Task.CompletedTask;
}
