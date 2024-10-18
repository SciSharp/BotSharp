using BotSharp.Plugin.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BotSharp.Plugin.EntityFrameworkCore.EntityConfigurations;

class ExecutionLogEntityTypeConfiguration
    : IEntityTypeConfiguration<ExecutionLog>
{
    private readonly string _tablePrefix;
    public ExecutionLogEntityTypeConfiguration(string tablePrefix)
    {
        _tablePrefix = tablePrefix;
    }
    public void Configure(EntityTypeBuilder<ExecutionLog> configuration)
    {
        configuration
            .ToTable($"{_tablePrefix}_ExecutionLogs");

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
