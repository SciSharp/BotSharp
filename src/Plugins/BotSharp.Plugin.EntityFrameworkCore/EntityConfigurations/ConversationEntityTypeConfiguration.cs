using BotSharp.Plugin.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BotSharp.Plugin.EntityFrameworkCore.EntityConfigurations;

class ConversationEntityTypeConfiguration
    : IEntityTypeConfiguration<Conversation>
{
    private readonly string _tablePrefix;
    public ConversationEntityTypeConfiguration(string tablePrefix)
    {
        _tablePrefix = tablePrefix;
    }
    public void Configure(EntityTypeBuilder<Conversation> configuration)
    {
        configuration
            .ToTable($"{_tablePrefix}_Conversations");

        configuration
            .HasIndex(a => a.Id);

        configuration
            .HasIndex(a => a.Title);

        configuration
            .HasIndex(a => a.TitleAlias);

        configuration
            .HasIndex(a => a.AgentId);

        configuration
            .HasIndex(a => a.CreatedTime);

        configuration
            .Property(a => a.Id)
            .HasMaxLength(64);
    }
}
