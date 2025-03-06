using BotSharp.Abstraction.Loggers.Models;

namespace BotSharp.Plugin.MongoStorage.Collections;

public class InstructionLogDocument : MongoBase
{
    public string? AgentId { get; set; }
    public string Provider { get; set; } = default!;
    public string Model { get; set; } = default!;
    public string? TemplateName { get; set; }
    public string UserMessage { get; set; } = default!;
    public string? SystemInstruction { get; set; }
    public string CompletionText { get; set; } = default!;
    public string? UserId { get; set; }
    public Dictionary<string, BsonDocument> States { get; set; } = new();
    public DateTime CreatedTime { get; set; }

    public static InstructionLogDocument ToMongoModel(InstructionLogModel log)
    {
        return new InstructionLogDocument
        {
            AgentId = log.AgentId,
            Provider = log.Provider,
            Model = log.Model,
            TemplateName = log.TemplateName,
            UserMessage = log.UserMessage,
            SystemInstruction = log.SystemInstruction,
            CompletionText = log.CompletionText,
            UserId = log.UserId,
            CreatedTime = log.CreatedTime
        };
    }

    public static InstructionLogModel ToDomainModel(InstructionLogDocument log)
    {
        return new InstructionLogModel
        {
            Id = log.Id,
            AgentId = log.AgentId,
            Provider = log.Provider,
            Model = log.Model,
            TemplateName = log.TemplateName,
            UserMessage = log.UserMessage,
            SystemInstruction = log.SystemInstruction,
            CompletionText = log.CompletionText,
            UserId = log.UserId,
            CreatedTime = log.CreatedTime
        };
    }
}
