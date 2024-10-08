using BotSharp.Plugin.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BotSharp.Plugin.EntityFrameworkCore.EntityConfigurations;

class LlmCompletionLogEntityTypeConfiguration
    : IEntityTypeConfiguration<LlmCompletionLog>
{
    private readonly string _tablePrefix;
    public LlmCompletionLogEntityTypeConfiguration(string tablePrefix)
    {
        _tablePrefix = tablePrefix;
    }
    public void Configure(EntityTypeBuilder<LlmCompletionLog> configuration)
    {
        configuration
            .ToTable($"{_tablePrefix}_LlmCompletionLogs");

        configuration
            .HasIndex(a => a.Id);

        configuration
            .HasIndex(a => a.ConversationId);

        configuration
            .Property(a => a.Id)
            .HasMaxLength(64);
    }
}
