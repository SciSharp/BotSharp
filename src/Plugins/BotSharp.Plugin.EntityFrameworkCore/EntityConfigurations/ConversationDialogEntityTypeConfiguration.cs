using BotSharp.Plugin.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BotSharp.Plugin.EntityFrameworkCore.EntityConfigurations;

class ConversationDialogEntityTypeConfiguration
    : IEntityTypeConfiguration<ConversationDialog>
{
    private readonly string _tablePrefix;
    public ConversationDialogEntityTypeConfiguration(string tablePrefix)
    {
        _tablePrefix = tablePrefix;
    }
    public void Configure(EntityTypeBuilder<ConversationDialog> configuration)
    {
        configuration
            .ToTable($"{_tablePrefix}_ConversationDialogs");

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
    }
}
