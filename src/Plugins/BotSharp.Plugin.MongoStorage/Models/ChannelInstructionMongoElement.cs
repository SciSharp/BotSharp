using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.Plugin.MongoStorage.Models;

public class ChannelInstructionMongoElement
{
    public string Channel { get; set; }
    public string Instruction { get; set; }

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
