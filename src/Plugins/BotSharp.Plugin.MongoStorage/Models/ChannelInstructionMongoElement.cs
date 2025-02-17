using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.Plugin.MongoStorage.Models;

[BsonIgnoreExtraElements(Inherited = true)]
public class ChannelInstructionMongoElement
{
    public string Channel { get; set; } = default!;
    public string Instruction { get; set; } = default!;

    public static ChannelInstructionMongoElement ToMongoElement(ChannelInstruction instruction)
    {
        return new ChannelInstructionMongoElement
        {
            Channel = instruction.Channel,
            Instruction = instruction.Instruction
        };
    }

    public static ChannelInstruction ToDomainElement(ChannelInstructionMongoElement instruction)
    {
        return new ChannelInstruction
        {
            Channel = instruction.Channel,
            Instruction = instruction.Instruction
        };
    }
}
