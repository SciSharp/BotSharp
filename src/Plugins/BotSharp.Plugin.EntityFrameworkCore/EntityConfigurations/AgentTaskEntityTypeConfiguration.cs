using BotSharp.Plugin.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BotSharp.Plugin.EntityFrameworkCore.EntityConfigurations;

class AgentTaskEntityTypeConfiguration
    : IEntityTypeConfiguration<AgentTask>
{
    private readonly string _tablePrefix;
    public AgentTaskEntityTypeConfiguration(string tablePrefix)
    {
        _tablePrefix = tablePrefix;
    }
    public void Configure(EntityTypeBuilder<AgentTask> configuration)
    {
        configuration
            .ToTable($"{_tablePrefix}_AgentTasks");

        configuration
            .HasIndex(a => a.Id);

        configuration
            .Property(a => a.Id)
            .HasMaxLength(64);
    }
}
