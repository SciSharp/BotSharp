using BotSharp.Plugin.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BotSharp.Plugin.EntityFrameworkCore.EntityConfigurations;

class TranslationMemoryEntityTypeConfiguration
    : IEntityTypeConfiguration<Entities.TranslationMemory>
{
    private readonly string _tablePrefix;
    public TranslationMemoryEntityTypeConfiguration(string tablePrefix)
    {
        _tablePrefix = tablePrefix;
    }
    public void Configure(EntityTypeBuilder<Entities.TranslationMemory> configuration)
    {
        configuration
            .ToTable($"{_tablePrefix}_TranslationMemorys");

        configuration
            .HasIndex(a => a.Id);

        configuration
            .Property(a => a.Id)
            .HasMaxLength(64);
    }
}
