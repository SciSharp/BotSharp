using BotSharp.Plugin.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BotSharp.Plugin.EntityFrameworkCore.EntityConfigurations;

class UserAgentEntityTypeConfiguration
    : IEntityTypeConfiguration<UserAgent>
{
    private readonly string _tablePrefix;
    public UserAgentEntityTypeConfiguration(string tablePrefix)
    {
        _tablePrefix = tablePrefix;
    }
    public void Configure(EntityTypeBuilder<UserAgent> configuration)
    {
        configuration
                .ToTable($"{_tablePrefix}_UserAgents");

        configuration
            .HasIndex(a => a.Id);

        configuration
            .Property(a => a.Id)
            .HasMaxLength(64);

        configuration
            .Property(a => a.AgentId)
            .HasMaxLength(64);

        configuration
            .Property(a => a.UserId)
            .HasMaxLength(64);

        configuration
            .HasIndex(a => a.CreatedTime);
    }
}
