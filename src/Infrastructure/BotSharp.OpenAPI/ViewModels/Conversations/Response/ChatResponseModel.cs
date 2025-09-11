using BotSharp.Abstraction.Conversations.Dtos;
using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Conversations;

public class ChatResponseModel : ChatResponseDto
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("additional_message_wrapper")]
    public ChatResponseWrapper? AdditionalMessageWrapper { get; set; }
}

public class ChatResponseWrapper
{
    [JsonPropertyName("sending_interval")]
    public int SendingInterval { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("messages")]
    public List<ChatResponseModel>? Messages { get; set; }

    public static ChatResponseWrapper? From(ChatMessageWrapper? wrapper, string conversationId, string? messageId = null)
    {
        if (wrapper == null)
        {
            return null;
        }

        return new ChatResponseWrapper
        {
            SendingInterval = wrapper.SendingInterval,
            Messages = wrapper?.Messages?.Select(x => new ChatResponseModel
            {
                ConversationId = conversationId,
                MessageId = messageId ?? x.MessageId,
                Text = !string.IsNullOrEmpty(x.SecondaryContent) ? x.SecondaryContent : x.Content,
                MessageLabel = x.MessageLabel,
                Function = x.FunctionName,
                RichContent = x.SecondaryRichContent ?? x.RichContent,
                Instruction = x.Instruction,
                Data = x.Data,
                IsAppend = true
            })?.ToList()
        };
    }
}