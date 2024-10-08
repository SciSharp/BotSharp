using BotSharp.Plugin.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace BotSharp.Plugin.EntityFrameworkCore.EntityConfigurations;

class ConversationStateLogEntityTypeConfiguration
    : IEntityTypeConfiguration<ConversationStateLog>
{
    private readonly string _tablePrefix;

    private JsonSerializerOptions _options;
    public ConversationStateLogEntityTypeConfiguration(string tablePrefix)
    {
        _tablePrefix = tablePrefix;

        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            AllowTrailingCommas = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
    }
    public void Configure(EntityTypeBuilder<ConversationStateLog> configuration)
    {
        configuration
            .ToTable($"{_tablePrefix}_ConversationStateLogs");

        var converter = new ValueConverter<Dictionary<string, string>, string>(
            v => JsonSerializer.Serialize(v, _options),
            v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, _options) ?? new Dictionary<string, string>());

        configuration
            .Property(p => p.States)
            .HasConversion(converter);

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
