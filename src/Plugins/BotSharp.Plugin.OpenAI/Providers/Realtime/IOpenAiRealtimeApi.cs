using BotSharp.Abstraction.Realtime.Models;
using Refit;

namespace BotSharp.Plugin.OpenAI.Providers.Realtime;

public interface IOpenAiRealtimeApi
{
    [Post("/v1/realtime/sessions")]
    Task<RealtimeSession> GetSessionAsync(RealtimeSessionRequest model, [Authorize("Bearer")] string token);
}
