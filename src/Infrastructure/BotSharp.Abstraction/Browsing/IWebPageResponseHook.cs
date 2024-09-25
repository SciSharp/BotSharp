using BotSharp.Abstraction.Browsing.Models;

namespace BotSharp.Abstraction.Browsing;

public interface IWebPageResponseHook
{
    void OnDataFetched(MessageInfo message, WebPageResponseData response);
    T? GetResponse<T>(MessageInfo message, WebPageResponseFilter filter);
}
