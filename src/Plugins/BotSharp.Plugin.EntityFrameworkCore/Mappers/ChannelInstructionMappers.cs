using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.Plugin.EntityFrameworkCore.Mappers;

public static class ChannelInstructionMappers
{
    public static Entities.ChannelInstruction ToEntity(this ChannelInstruction model)
    {
        return new Entities.ChannelInstruction
        {
            Channel = model.Channel,
            Instruction = model.Instruction
        };
    }

    public static ChannelInstruction ToModel(this Entities.ChannelInstruction model)
    {
        return new ChannelInstruction
        {
            Channel = model.Channel,
            Instruction = model.Instruction
        };
    }
}
