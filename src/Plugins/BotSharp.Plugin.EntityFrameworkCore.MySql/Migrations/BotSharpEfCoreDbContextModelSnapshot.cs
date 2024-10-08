﻿// <auto-generated />
using System;
using BotSharp.Plugin.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace BotSharp.Plugin.EntityFrameworkCore.MySql.Migrations
{
    [DbContext(typeof(BotSharpEfCoreDbContext))]
    partial class BotSharpEfCoreDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.10")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            MySqlModelBuilderExtensions.AutoIncrementColumns(modelBuilder);

            modelBuilder.Entity("BotSharp.Plugin.EntityFrameworkCore.Entities.Agent", b =>
                {
                    b.Property<string>("Id")
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<string>("ChannelInstructions")
                        .IsRequired()
                        .HasColumnType("json");

                    b.Property<DateTime>("CreatedTime")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<bool>("Disabled")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Functions")
                        .IsRequired()
                        .HasColumnType("json");

                    b.Property<string>("IconUrl")
                        .HasColumnType("longtext");

                    b.Property<string>("InheritAgentId")
                        .HasColumnType("longtext");

                    b.Property<string>("Instruction")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<bool>("IsPublic")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("LlmConfig")
                        .HasColumnType("json");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Profiles")
                        .IsRequired()
                        .HasColumnType("json");

                    b.Property<string>("Responses")
                        .IsRequired()
                        .HasColumnType("json");

                    b.Property<string>("RoutingRules")
                        .IsRequired()
                        .HasColumnType("json");

                    b.Property<string>("Samples")
                        .IsRequired()
                        .HasColumnType("json");

                    b.Property<string>("Templates")
                        .IsRequired()
                        .HasColumnType("json");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime>("UpdatedTime")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Utilities")
                        .IsRequired()
                        .HasColumnType("json");

                    b.HasKey("Id");

                    b.HasIndex("Id");

                    b.ToTable("BotSharp_Agents", (string)null);
                });

            modelBuilder.Entity("BotSharp.Plugin.EntityFrameworkCore.Entities.AgentTask", b =>
                {
                    b.Property<string>("Id")
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<string>("AgentId")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime>("CreatedTime")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Description")
                        .HasColumnType("longtext");

                    b.Property<string>("DirectAgentId")
                        .HasColumnType("longtext");

                    b.Property<bool>("Enabled")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime>("UpdatedTime")
                        .HasColumnType("datetime(6)");

                    b.HasKey("Id");

                    b.HasIndex("Id");

                    b.ToTable("BotSharp_AgentTasks", (string)null);
                });

            modelBuilder.Entity("BotSharp.Plugin.EntityFrameworkCore.Entities.Conversation", b =>
                {
                    b.Property<string>("Id")
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<string>("AgentId")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<string>("Channel")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime>("CreatedTime")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("DialogCount")
                        .HasColumnType("int");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("TaskId")
                        .HasColumnType("longtext");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<string>("TitleAlias")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<DateTime>("UpdatedTime")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.HasIndex("AgentId");

                    b.HasIndex("CreatedTime");

                    b.HasIndex("Id");

                    b.HasIndex("Title");

                    b.HasIndex("TitleAlias");

                    b.ToTable("BotSharp_Conversations", (string)null);
                });

            modelBuilder.Entity("BotSharp.Plugin.EntityFrameworkCore.Entities.ConversationContentLog", b =>
                {
                    b.Property<string>("Id")
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<string>("AgentId")
                        .HasColumnType("longtext");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("ConversationId")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<DateTime>("CreatedTime")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("MessageId")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Name")
                        .HasColumnType("longtext");

                    b.Property<string>("Role")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Source")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.HasIndex("ConversationId");

                    b.HasIndex("CreatedTime");

                    b.HasIndex("Id");

                    b.ToTable("BotSharp_ConversationContentLogs", (string)null);
                });

            modelBuilder.Entity("BotSharp.Plugin.EntityFrameworkCore.Entities.ConversationDialog", b =>
                {
                    b.Property<string>("Id")
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<string>("ConversationId")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<string>("Dialogs")
                        .IsRequired()
                        .HasColumnType("json");

                    b.HasKey("Id");

                    b.HasIndex("ConversationId");

                    b.HasIndex("Id");

                    b.ToTable("BotSharp_ConversationDialogs", (string)null);
                });

            modelBuilder.Entity("BotSharp.Plugin.EntityFrameworkCore.Entities.ConversationState", b =>
                {
                    b.Property<string>("Id")
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<string>("Breakpoints")
                        .IsRequired()
                        .HasColumnType("json");

                    b.Property<string>("ConversationId")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.HasKey("Id");

                    b.HasIndex("ConversationId");

                    b.HasIndex("Id");

                    b.ToTable("BotSharp_ConversationStates", (string)null);
                });

            modelBuilder.Entity("BotSharp.Plugin.EntityFrameworkCore.Entities.ConversationStateLog", b =>
                {
                    b.Property<string>("Id")
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<string>("ConversationId")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<DateTime>("CreatedTime")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("MessageId")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("States")
                        .IsRequired()
                        .HasColumnType("json");

                    b.HasKey("Id");

                    b.HasIndex("ConversationId");

                    b.HasIndex("CreatedTime");

                    b.HasIndex("Id");

                    b.ToTable("BotSharp_ConversationStateLogs", (string)null);
                });

            modelBuilder.Entity("BotSharp.Plugin.EntityFrameworkCore.Entities.ExecutionLog", b =>
                {
                    b.Property<string>("Id")
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<string>("ConversationId")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<string>("Logs")
                        .IsRequired()
                        .HasColumnType("json");

                    b.HasKey("Id");

                    b.HasIndex("ConversationId");

                    b.HasIndex("Id");

                    b.ToTable("BotSharp_ExecutionLogs", (string)null);
                });

            modelBuilder.Entity("BotSharp.Plugin.EntityFrameworkCore.Entities.LlmCompletionLog", b =>
                {
                    b.Property<string>("Id")
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<string>("ConversationId")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<string>("Logs")
                        .IsRequired()
                        .HasColumnType("json");

                    b.HasKey("Id");

                    b.HasIndex("ConversationId");

                    b.HasIndex("Id");

                    b.ToTable("BotSharp_LlmCompletionLogs", (string)null);
                });

            modelBuilder.Entity("BotSharp.Plugin.EntityFrameworkCore.Entities.Plugin", b =>
                {
                    b.Property<string>("Id")
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<string>("EnabledPlugins")
                        .IsRequired()
                        .HasColumnType("json");

                    b.HasKey("Id");

                    b.HasIndex("Id");

                    b.ToTable("BotSharp_Plugins", (string)null);
                });

            modelBuilder.Entity("BotSharp.Plugin.EntityFrameworkCore.Entities.State", b =>
                {
                    b.Property<string>("Id")
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<string>("ConversationStateId")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<string>("Key")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<bool>("Readonly")
                        .HasColumnType("tinyint(1)");

                    b.Property<bool>("Versioning")
                        .HasColumnType("tinyint(1)");

                    b.HasKey("Id");

                    b.HasIndex("ConversationStateId");

                    b.HasIndex("Id");

                    b.HasIndex("Key");

                    b.ToTable("BotSharp_States", (string)null);
                });

            modelBuilder.Entity("BotSharp.Plugin.EntityFrameworkCore.Entities.StateValue", b =>
                {
                    b.Property<string>("Id")
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<bool>("Active")
                        .HasColumnType("tinyint(1)");

                    b.Property<int>("ActiveRounds")
                        .HasColumnType("int");

                    b.Property<string>("Data")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<string>("DataType")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("varchar(128)");

                    b.Property<string>("MessageId")
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<string>("Source")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("varchar(128)");

                    b.Property<string>("StateId")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<DateTime>("UpdateTime")
                        .HasColumnType("datetime(6)");

                    b.HasKey("Id");

                    b.HasIndex("Data")
                        .HasDatabaseName("IX_StateValues_Data");

                    b.HasIndex("Id")
                        .IsUnique()
                        .HasDatabaseName("IX_StateValues_Id");

                    b.HasIndex("MessageId")
                        .HasDatabaseName("IX_StateValues_MessageId");

                    b.HasIndex("StateId")
                        .HasDatabaseName("IX_StateValues_StateId");

                    b.ToTable("BotSharp_StateValues", (string)null);
                });

            modelBuilder.Entity("BotSharp.Plugin.EntityFrameworkCore.Entities.TranslationMemory", b =>
                {
                    b.Property<string>("Id")
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<string>("HashText")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("OriginalText")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Translations")
                        .IsRequired()
                        .HasColumnType("json");

                    b.HasKey("Id");

                    b.HasIndex("Id");

                    b.ToTable("BotSharp_TranslationMemorys", (string)null);
                });

            modelBuilder.Entity("BotSharp.Plugin.EntityFrameworkCore.Entities.User", b =>
                {
                    b.Property<string>("Id")
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<DateTime>("CreatedTime")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Email")
                        .HasColumnType("longtext");

                    b.Property<string>("ExternalId")
                        .HasColumnType("longtext");

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("LastName")
                        .HasColumnType("longtext");

                    b.Property<string>("Password")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Phone")
                        .HasColumnType("longtext");

                    b.Property<string>("Role")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Salt")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Source")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime>("UpdatedTime")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("UserName")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("VerificationCode")
                        .HasColumnType("longtext");

                    b.Property<bool>("Verified")
                        .HasColumnType("tinyint(1)");

                    b.HasKey("Id");

                    b.HasIndex("CreatedTime");

                    b.HasIndex("Id");

                    b.ToTable("BotSharp_Users", (string)null);
                });

            modelBuilder.Entity("BotSharp.Plugin.EntityFrameworkCore.Entities.UserAgent", b =>
                {
                    b.Property<string>("Id")
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<string>("AgentId")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<DateTime>("CreatedTime")
                        .HasColumnType("datetime(6)");

                    b.Property<bool>("Editable")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTime>("UpdatedTime")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.HasKey("Id");

                    b.HasIndex("CreatedTime");

                    b.HasIndex("Id");

                    b.ToTable("BotSharp_UserAgents", (string)null);
                });

            modelBuilder.Entity("BotSharp.Plugin.EntityFrameworkCore.Entities.State", b =>
                {
                    b.HasOne("BotSharp.Plugin.EntityFrameworkCore.Entities.ConversationState", "ConversationState")
                        .WithMany("States")
                        .HasForeignKey("ConversationStateId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ConversationState");
                });

            modelBuilder.Entity("BotSharp.Plugin.EntityFrameworkCore.Entities.StateValue", b =>
                {
                    b.HasOne("BotSharp.Plugin.EntityFrameworkCore.Entities.State", "State")
                        .WithMany("Values")
                        .HasForeignKey("StateId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("State");
                });

            modelBuilder.Entity("BotSharp.Plugin.EntityFrameworkCore.Entities.ConversationState", b =>
                {
                    b.Navigation("States");
                });

            modelBuilder.Entity("BotSharp.Plugin.EntityFrameworkCore.Entities.State", b =>
                {
                    b.Navigation("Values");
                });
#pragma warning restore 612, 618
        }
    }
}
