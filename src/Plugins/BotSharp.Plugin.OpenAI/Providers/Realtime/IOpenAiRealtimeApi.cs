using BotSharp.Abstraction.Realtime.Models;
using BotSharp.Plugin.OpenAI.Models.Realtime;
using Refit;

namespace BotSharp.Plugin.OpenAI.Providers.Realtime;

public interface IOpenAiRealtimeApi
{
    [Post("/v1/realtime/sessions")]
    Task<RealtimeSession> GetSessionAsync(RealtimeSessionCreationRequest model, [Authorize("Bearer")] string token);
}
