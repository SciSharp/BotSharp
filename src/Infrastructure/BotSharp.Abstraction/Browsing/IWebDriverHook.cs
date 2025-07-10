using BotSharp.Abstraction.Browsing.Models;

namespace BotSharp.Abstraction.Browsing;

public interface IWebDriverHook
{
    Task<List<string>> GetUploadFiles(MessageInfo message);
    Task OnLocateElement(MessageInfo message, string content);
}
