using System.Net.Http.Headers;

namespace BotSharp.Abstraction.Http;

public interface IHttpRequestHook
{
    Task OnAddHttpHeaders(HttpHeaders headers, Uri uri);
}
