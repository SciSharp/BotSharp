using BotSharp.Abstraction.Messaging;
using BotSharp.Abstraction.Messaging.Models.RichContent;

namespace BotSharp.Core.Messaging;

public class RichContentService : IRichContentService
{
    public List<IRichMessage> ConvertToMessages(string content)
    {
        var messages = new List<IRichMessage>();
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        var tempMessages = JsonSerializer.Deserialize<JsonDocument[]>(content, options);

        foreach (var m in tempMessages)
        {
            var richType = "text";
            if (m.RootElement.TryGetProperty("rich_type", out var element))
            {
                richType = element.GetString();
            }
            
            if (richType == "text")
            {
                messages.Add(JsonSerializer.Deserialize<TextMessage>(m.RootElement.ToString(), options));
            }
            else if (richType == "quick_reply")
            {
                messages.Add(JsonSerializer.Deserialize<QuickReplyMessage>(m.RootElement.ToString(), options));
            }
        }

        return messages.ToList();
    }
}
