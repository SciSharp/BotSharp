using System;
using System.Collections.Generic;
using BotSharp.Plugin.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BotSharp.Plugin.EntityFrameworkCore.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BotSharp_Agents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    InheritAgentId = table.Column<string>(type: "text", nullable: true),
                    IconUrl = table.Column<string>(type: "text", nullable: true),
                    Instruction = table.Column<string>(type: "text", nullable: false),
                    ChannelInstructions = table.Column<List<ChannelInstruction>>(type: "json", nullable: false),
                    Templates = table.Column<List<AgentTemplate>>(type: "json", nullable: false),
                    Functions = table.Column<List<FunctionDef>>(type: "json", nullable: false),
                    Responses = table.Column<List<AgentResponse>>(type: "json", nullable: false),
                    Samples = table.Column<List<string>>(type: "json", nullable: false),
                    Utilities = table.Column<List<string>>(type: "json", nullable: false),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    Disabled = table.Column<bool>(type: "boolean", nullable: false),
                    Profiles = table.Column<List<string>>(type: "json", nullable: false),
                    RoutingRules = table.Column<List<RoutingRule>>(type: "json", nullable: false),
                    LlmConfig = table.Column<AgentLlmConfig>(type: "json", nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotSharp_Agents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BotSharp_AgentTasks",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    AgentId = table.Column<string>(type: "text", nullable: false),
                    DirectAgentId = table.Column<string>(type: "text", nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotSharp_AgentTasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BotSharp_ConversationContentLogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ConversationId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MessageId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    AgentId = table.Column<string>(type: "text", nullable: true),
                    Role = table.Column<string>(type: "text", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotSharp_ConversationContentLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BotSharp_ConversationDialogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ConversationId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Dialogs = table.Column<List<Dialog>>(type: "json", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotSharp_ConversationDialogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BotSharp_Conversations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AgentId = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    TaskId = table.Column<string>(type: "text", nullable: true),
                    Title = table.Column<string>(type: "text", nullable: false),
                    TitleAlias = table.Column<string>(type: "text", nullable: false),
                    Channel = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    DialogCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotSharp_Conversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BotSharp_ConversationStateLogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ConversationId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MessageId = table.Column<string>(type: "text", nullable: false),
                    States = table.Column<string>(type: "json", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotSharp_ConversationStateLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BotSharp_ConversationStates",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ConversationId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Breakpoints = table.Column<List<BreakpointInfo>>(type: "json", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotSharp_ConversationStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BotSharp_ExecutionLogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ConversationId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Logs = table.Column<List<string>>(type: "json", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotSharp_ExecutionLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BotSharp_LlmCompletionLogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ConversationId = table.Column<string>(type: "text", nullable: false),
                    Logs = table.Column<List<PromptLog>>(type: "json", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotSharp_LlmCompletionLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BotSharp_Plugins",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EnabledPlugins = table.Column<List<string>>(type: "json", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotSharp_Plugins", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BotSharp_TranslationMemorys",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OriginalText = table.Column<string>(type: "text", nullable: false),
                    HashText = table.Column<string>(type: "text", nullable: false),
                    Translations = table.Column<List<TranslationMemoryInfo>>(type: "json", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotSharp_TranslationMemorys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BotSharp_UserAgents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AgentId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Editable = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotSharp_UserAgents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BotSharp_Users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Salt = table.Column<string>(type: "text", nullable: false),
                    Password = table.Column<string>(type: "text", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    ExternalId = table.Column<string>(type: "text", nullable: true),
                    Role = table.Column<string>(type: "text", nullable: false),
                    VerificationCode = table.Column<string>(type: "text", nullable: true),
                    Verified = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotSharp_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BotSharp_States",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Key = table.Column<string>(type: "text", nullable: false),
                    Versioning = table.Column<bool>(type: "boolean", nullable: false),
                    Readonly = table.Column<bool>(type: "boolean", nullable: false),
                    ConversationStateId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotSharp_States", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BotSharp_States_BotSharp_ConversationStates_ConversationSta~",
                        column: x => x.ConversationStateId,
                        principalTable: "BotSharp_ConversationStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BotSharp_StateValues",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Data = table.Column<string>(type: "text", nullable: false),
                    MessageId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    ActiveRounds = table.Column<int>(type: "integer", nullable: false),
                    DataType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Source = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StateId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotSharp_StateValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BotSharp_StateValues_BotSharp_States_StateId",
                        column: x => x.StateId,
                        principalTable: "BotSharp_States",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BotSharp_Agents_Id",
                table: "BotSharp_Agents",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_BotSharp_AgentTasks_Id",
                table: "BotSharp_AgentTasks",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_BotSharp_ConversationContentLogs_ConversationId",
                table: "BotSharp_ConversationContentLogs",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_BotSharp_ConversationContentLogs_CreatedTime",
                table: "BotSharp_ConversationContentLogs",
                column: "CreatedTime");

            migrationBuilder.CreateIndex(
                name: "IX_BotSharp_ConversationContentLogs_Id",
                table: "BotSharp_ConversationContentLogs",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_BotSharp_ConversationDialogs_ConversationId",
                table: "BotSharp_ConversationDialogs",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_BotSharp_ConversationDialogs_Id",
                table: "BotSharp_ConversationDialogs",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_BotSharp_Conversations_AgentId",
                table: "BotSharp_Conversations",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_BotSharp_Conversations_CreatedTime",
                table: "BotSharp_Conversations",
                column: "CreatedTime");

            migrationBuilder.CreateIndex(
                name: "IX_BotSharp_Conversations_Id",
                table: "BotSharp_Conversations",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_BotSharp_Conversations_Title",
                table: "BotSharp_Conversations",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_BotSharp_Conversations_TitleAlias",
                table: "BotSharp_Conversations",
                column: "TitleAlias");

            migrationBuilder.CreateIndex(
                name: "IX_BotSharp_ConversationStateLogs_ConversationId",
                table: "BotSharp_ConversationStateLogs",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_BotSharp_ConversationStateLogs_CreatedTime",
                table: "BotSharp_ConversationStateLogs",
                column: "CreatedTime");

            migrationBuilder.CreateIndex(
                name: "IX_BotSharp_ConversationStateLogs_Id",
                table: "BotSharp_ConversationStateLogs",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_BotSharp_ConversationStates_ConversationId",
                table: "BotSharp_ConversationStates",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_BotSharp_ConversationStates_Id",
                table: "BotSharp_ConversationStates",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_BotSharp_ExecutionLogs_ConversationId",
                table: "BotSharp_ExecutionLogs",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_BotSharp_ExecutionLogs_Id",
                table: "BotSharp_ExecutionLogs",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_BotSharp_LlmCompletionLogs_ConversationId",
                table: "BotSharp_LlmCompletionLogs",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_BotSharp_LlmCompletionLogs_Id",
                table: "BotSharp_LlmCompletionLogs",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_BotSharp_Plugins_Id",
                table: "BotSharp_Plugins",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_BotSharp_States_ConversationStateId",
                table: "BotSharp_States",
                column: "ConversationStateId");

            migrationBuilder.CreateIndex(
                name: "IX_BotSharp_States_Id",
                table: "BotSharp_States",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_BotSharp_States_Key",
                table: "BotSharp_States",
                column: "Key");

            migrationBuilder.CreateIndex(
                name: "IX_StateValues_Data",
                table: "BotSharp_StateValues",
                column: "Data");

            migrationBuilder.CreateIndex(
                name: "IX_StateValues_Id",
                table: "BotSharp_StateValues",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StateValues_MessageId",
                table: "BotSharp_StateValues",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_StateValues_StateId",
                table: "BotSharp_StateValues",
                column: "StateId");

            migrationBuilder.CreateIndex(
                name: "IX_BotSharp_TranslationMemorys_Id",
                table: "BotSharp_TranslationMemorys",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_BotSharp_UserAgents_CreatedTime",
                table: "BotSharp_UserAgents",
                column: "CreatedTime");

            migrationBuilder.CreateIndex(
                name: "IX_BotSharp_UserAgents_Id",
                table: "BotSharp_UserAgents",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_BotSharp_Users_CreatedTime",
                table: "BotSharp_Users",
                column: "CreatedTime");

            migrationBuilder.CreateIndex(
                name: "IX_BotSharp_Users_Id",
                table: "BotSharp_Users",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BotSharp_Agents");

            migrationBuilder.DropTable(
                name: "BotSharp_AgentTasks");

            migrationBuilder.DropTable(
                name: "BotSharp_ConversationContentLogs");

            migrationBuilder.DropTable(
                name: "BotSharp_ConversationDialogs");

            migrationBuilder.DropTable(
                name: "BotSharp_Conversations");

            migrationBuilder.DropTable(
                name: "BotSharp_ConversationStateLogs");

            migrationBuilder.DropTable(
                name: "BotSharp_ExecutionLogs");

            migrationBuilder.DropTable(
                name: "BotSharp_LlmCompletionLogs");

            migrationBuilder.DropTable(
                name: "BotSharp_Plugins");

            migrationBuilder.DropTable(
                name: "BotSharp_StateValues");

            migrationBuilder.DropTable(
                name: "BotSharp_TranslationMemorys");

            migrationBuilder.DropTable(
                name: "BotSharp_UserAgents");

            migrationBuilder.DropTable(
                name: "BotSharp_Users");

            migrationBuilder.DropTable(
                name: "BotSharp_States");

            migrationBuilder.DropTable(
                name: "BotSharp_ConversationStates");
        }
    }
}
