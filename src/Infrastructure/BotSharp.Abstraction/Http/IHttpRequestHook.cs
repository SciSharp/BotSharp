using System.Net.Http.Headers;

namespace BotSharp.Abstraction.Http;

public interface IHttpRequestHook
{
    void OnAddHttpHeaders(HttpHeaders headers);
}
