using BotSharp.Plugin.EntityFrameworkCore.Entities;
using BotSharp.Plugin.EntityFrameworkCore.EntityConfigurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace BotSharp.Plugin.EntityFrameworkCore;

public class BotSharpEfCoreDbContext : DbContext
{
    public DbSet<Agent> Agents { get; set; }
    public DbSet<AgentTask> AgentTasks { get; set; }
    public DbSet<ConversationContentLog> ConversationContentLogs { get; set; }
    public DbSet<ConversationDialog> ConversationDialogs { get; set; }
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<ConversationState> ConversationStates { get; set; }
    public DbSet<State> States { get; set; }
    public DbSet<StateValue> StateValues { get; set; }
    public DbSet<ConversationStateLog> ConversationStateLogs { get; set; }
    public DbSet<ExecutionLog> ExecutionLogs { get; set; }
    public DbSet<LlmCompletionLog> LlmCompletionLogs { get; set; }
    public DbSet<Entities.Plugin> Plugins { get; set; }
    public DbSet<Entities.TranslationMemory> TranslationMemorys { get; set; }
    public DbSet<UserAgent> UserAgents { get; set; }
    public DbSet<User> Users { get; set; }
    public BotSharpEfCoreDbContext(DbContextOptions<BotSharpEfCoreDbContext> options) : base(options) { }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var dbSettings = this.GetService<BotSharpDatabaseSettings>();

        var tablePrefix = dbSettings.TablePrefix.IfNullOrEmptyAs("BotSharp");

        modelBuilder.ApplyConfiguration(new AgentEntityTypeConfiguration(tablePrefix));

        modelBuilder.ApplyConfiguration(new AgentTaskEntityTypeConfiguration(tablePrefix));

        modelBuilder.ApplyConfiguration(new ConversationContentLogEntityTypeConfiguration(tablePrefix));

        modelBuilder.ApplyConfiguration(new ConversationDialogEntityTypeConfiguration(tablePrefix));

        modelBuilder.ApplyConfiguration(new ConversationEntityTypeConfiguration(tablePrefix));

        modelBuilder.ApplyConfiguration(new ConversationStateEntityTypeConfiguration(tablePrefix));

        modelBuilder.ApplyConfiguration(new StateEntityTypeConfiguration(tablePrefix));

        modelBuilder.ApplyConfiguration(new StateValueEntityTypeConfiguration(tablePrefix));

        modelBuilder.ApplyConfiguration(new ConversationStateLogEntityTypeConfiguration(tablePrefix));

        modelBuilder.ApplyConfiguration(new ExecutionLogEntityTypeConfiguration(tablePrefix));

        modelBuilder.ApplyConfiguration(new LlmCompletionLogEntityTypeConfiguration(tablePrefix));

        modelBuilder.ApplyConfiguration(new PluginEntityTypeConfiguration(tablePrefix));

        modelBuilder.ApplyConfiguration(new TranslationMemoryEntityTypeConfiguration(tablePrefix));

        modelBuilder.ApplyConfiguration(new UserAgentEntityTypeConfiguration(tablePrefix));

        modelBuilder.ApplyConfiguration(new UserEntityTypeConfiguration(tablePrefix));
    }
}
