namespace BotSharp.Abstraction.Infrastructures.MessageQueues.Models;

public class MQMessage<T>
{
    public MQMessage(T payload, string messageId)
    {
        Payload = payload;
        MessageId = messageId;
    }

    public T Payload { get; set; }
    public string MessageId { get; set; }
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;
}
