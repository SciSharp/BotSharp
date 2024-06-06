namespace BotSharp.Plugin.MetaGLM.Models.RequestModels;

public class MessageItem
{
    public string role { get; set; }
    public string content { get; set; }

    public MessageItem(string role, string content)
    {
        this.role = role;
        this.content = content;
    }
    
    public MessageItem()
    {
    }
}