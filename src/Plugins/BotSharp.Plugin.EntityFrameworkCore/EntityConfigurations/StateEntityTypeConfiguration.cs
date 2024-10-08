using BotSharp.Plugin.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BotSharp.Plugin.EntityFrameworkCore.EntityConfigurations;

class StateEntityTypeConfiguration
    : IEntityTypeConfiguration<State>
{
    private readonly string _tablePrefix;
    public StateEntityTypeConfiguration(string tablePrefix)
    {
        _tablePrefix = tablePrefix;
    }
    public void Configure(EntityTypeBuilder<State> configuration)
    {
        configuration
            .ToTable($"{_tablePrefix}_States");

        configuration
            .HasIndex(a => a.Id);

        configuration
            .HasIndex(a => a.Key);

        configuration
            .Property(a => a.Id)
            .HasMaxLength(64);

        configuration
            .HasIndex(a => a.ConversationStateId);

        configuration
            .Property(a => a.ConversationStateId)
            .HasMaxLength(64);
    }
}
