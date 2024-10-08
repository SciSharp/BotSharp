using BotSharp.Plugin.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BotSharp.Plugin.EntityFrameworkCore.EntityConfigurations;

class StateValueEntityTypeConfiguration
    : IEntityTypeConfiguration<StateValue>
{
    private readonly string _tablePrefix;
    public StateValueEntityTypeConfiguration(string tablePrefix)
    {
        _tablePrefix = tablePrefix;
    }
    public void Configure(EntityTypeBuilder<StateValue> configuration)
    {
        configuration
                  .ToTable($"{_tablePrefix}_StateValues");

        configuration
            .HasIndex(a => a.Id)
            .HasDatabaseName("IX_StateValues_Id")
            .IsUnique();

        configuration
            .HasIndex(a => a.Data)
            .HasDatabaseName("IX_StateValues_Data");

        configuration
            .HasIndex(a => a.MessageId)
            .HasDatabaseName("IX_StateValues_MessageId");

        configuration
            .Property(a => a.MessageId)
            .HasMaxLength(64);

        configuration
            .Property(a => a.DataType)
            .HasMaxLength(128);

        configuration
            .Property(a => a.Source)
            .HasMaxLength(128);

        configuration
            .Property(a => a.Id)
            .HasMaxLength(64);

        configuration
            .HasIndex(a => a.StateId)
            .HasDatabaseName("IX_StateValues_StateId");

        configuration
            .Property(a => a.StateId)
            .HasMaxLength(64);
    }
}
