using BotSharp.Plugin.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BotSharp.Plugin.EntityFrameworkCore.EntityConfigurations;

class ConversationContentLogEntityTypeConfiguration
    : IEntityTypeConfiguration<ConversationContentLog>
{
    private readonly string _tablePrefix;
    public ConversationContentLogEntityTypeConfiguration(string tablePrefix)
    {
        _tablePrefix = tablePrefix;
    }
    public void Configure(EntityTypeBuilder<ConversationContentLog> configuration)
    {
        configuration
            .ToTable($"{_tablePrefix}_ConversationContentLogs");

        configuration
            .HasIndex(a => a.Id);

        configuration
            .Property(a => a.Id)
            .HasMaxLength(64);

        configuration
            .HasIndex(a => a.ConversationId);

        configuration
            .Property(a => a.ConversationId)
            .HasMaxLength(64);

        configuration
            .HasIndex(a => a.CreatedTime);
    }
}
