using BotSharp.Abstraction.Loggers.Models;
using System.Text.Json;

namespace BotSharp.Plugin.MongoStorage.Collections;

public class InstructionLogBetaDocument : MongoBase
{
    public string? AgentId { get; set; }
    public string Provider { get; set; } = default!;
    public string Model { get; set; } = default!;
    public string? UserId { get; set; }
    public Dictionary<string, BsonDocument> States { get; set; } = new();
    public DateTime CreatedTime { get; set; }

    public static InstructionLogBetaDocument ToMongoModel(InstructionLogModel log)
    {
        return new InstructionLogBetaDocument
        {
            AgentId = log.AgentId,
            Provider = log.Provider,
            Model = log.Model,
            UserId = log.UserId,
            CreatedTime = log.CreatedTime
        };
    }

    public static InstructionLogModel ToDomainModel(InstructionLogBetaDocument log)
    {
        return new InstructionLogModel
        {
            AgentId = log.AgentId,
            Provider = log.Provider,
            Model = log.Model,
            UserId = log.UserId,
            CreatedTime = log.CreatedTime
        };
    }
}
