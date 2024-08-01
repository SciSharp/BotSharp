using BotSharp.Abstraction.Browsing.Models;

namespace BotSharp.Abstraction.Browsing;

public interface IWebPageResponseHook
{
    void OnDataFetched(MessageInfo message, string url, string postData, string responsData);
    T? GetResponse<T>(MessageInfo message, string url, string? queryParameter = null);
}
