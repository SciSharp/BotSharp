using BotSharp.Plugin.Twilio.Models;
using Task = System.Threading.Tasks.Task;

namespace BotSharp.Plugin.Twilio.Interfaces;

public interface ITwilioSessionHook
{
    /// <summary>
    /// Before session creating
    /// </summary>
    /// <param name="request"></param>
    /// <param name="response"></param>
    /// <returns></returns>
    Task OnSessionCreating(ConversationalVoiceRequest request, ConversationalVoiceResponse response)
        => Task.CompletedTask;

    /// <summary>
    /// On session created
    /// </summary>
    /// <param name="request"></param>
    /// <param name="response"></param>
    /// <returns></returns>
    Task OnSessionCreated(ConversationalVoiceRequest request)
        => Task.CompletedTask;

    /// <summary>
    /// On received user message
    /// </summary>
    /// <param name="request"></param>
    /// <param name="response"></param>
    /// <returns></returns>
    Task OnReceivedUserMessage(ConversationalVoiceRequest request)
        => Task.CompletedTask;

    /// <summary>
    /// Waiting user response
    /// </summary>
    /// <param name="request"></param>
    /// <param name="response"></param>
    /// <returns></returns>
    Task OnWaitingUserResponse(ConversationalVoiceRequest request, ConversationalVoiceResponse response)
        => Task.CompletedTask;

    /// <summary>
    /// On agent generated indication
    /// </summary>
    /// <param name="request"></param>
    /// <param name="response"></param>
    /// <returns></returns>
    Task OnIndicationGenerated(ConversationalVoiceRequest request, ConversationalVoiceResponse response)
        => Task.CompletedTask;

    /// <summary>
    /// Waiting agent response
    /// </summary>
    /// <param name="request"></param>
    /// <param name="response"></param>
    /// <returns></returns>
    Task OnWaitingAgentResponse(ConversationalVoiceRequest request, ConversationalVoiceResponse response)
        => Task.CompletedTask;

    /// <summary>
    /// Before agent responsing
    /// </summary>
    /// <param name="request"></param>
    /// <param name="response"></param>
    /// <returns></returns>
    Task OnAgentResponsing(ConversationalVoiceRequest request, ConversationalVoiceResponse response)
        => Task.CompletedTask;

    /// <summary>
    /// On agent hang up
    /// </summary>
    /// <param name="request"></param>
    /// <param name="response"></param>
    /// <returns></returns>
    Task OnAgentHangUp(ConversationalVoiceRequest request)
        => Task.CompletedTask;

    /// <summary>
    /// Before agent transferred
    /// </summary>
    /// <param name="request"></param>
    /// <param name="response"></param>
    /// <returns></returns>
    Task OnAgentTransferring(ConversationalVoiceRequest request, TwilioSetting settings)
        => Task.CompletedTask;
}
