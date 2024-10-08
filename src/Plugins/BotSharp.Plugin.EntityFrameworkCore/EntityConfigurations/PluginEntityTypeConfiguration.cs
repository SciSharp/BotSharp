using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BotSharp.Plugin.EntityFrameworkCore.EntityConfigurations;

class PluginEntityTypeConfiguration
    : IEntityTypeConfiguration<Entities.Plugin>
{
    private readonly string _tablePrefix;
    public PluginEntityTypeConfiguration(string tablePrefix)
    {
        _tablePrefix = tablePrefix;
    }
    public void Configure(EntityTypeBuilder<Entities.Plugin> configuration)
    {
        configuration
            .ToTable($"{_tablePrefix}_Plugins");

        configuration
            .HasIndex(a => a.Id);

        configuration
            .Property(a => a.Id)
            .HasMaxLength(64);
    }
}
