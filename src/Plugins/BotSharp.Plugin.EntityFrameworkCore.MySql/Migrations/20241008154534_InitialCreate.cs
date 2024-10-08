using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BotSharp.Plugin.EntityFrameworkCore.MySql.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BotSharp_Agents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InheritAgentId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IconUrl = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Instruction = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChannelInstructions = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Templates = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Functions = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Responses = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Samples = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Utilities = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsPublic = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Disabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Profiles = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RoutingRules = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LlmConfig = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotSharp_Agents", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BotSharp_AgentTasks",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Content = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Enabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AgentId = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DirectAgentId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotSharp_AgentTasks", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BotSharp_ConversationContentLogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConversationId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MessageId = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AgentId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Role = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Source = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Content = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotSharp_ConversationContentLogs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BotSharp_ConversationDialogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConversationId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Dialogs = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotSharp_ConversationDialogs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BotSharp_Conversations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AgentId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TaskId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Title = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TitleAlias = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Channel = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DialogCount = table.Column<int>(type: "int", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotSharp_Conversations", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BotSharp_ConversationStateLogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConversationId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MessageId = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    States = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotSharp_ConversationStateLogs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BotSharp_ConversationStates",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConversationId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Breakpoints = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotSharp_ConversationStates", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BotSharp_ExecutionLogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConversationId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Logs = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotSharp_ExecutionLogs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BotSharp_LlmCompletionLogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConversationId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Logs = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotSharp_LlmCompletionLogs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BotSharp_Plugins",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EnabledPlugins = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotSharp_Plugins", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BotSharp_TranslationMemorys",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OriginalText = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HashText = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Translations = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotSharp_TranslationMemorys", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BotSharp_UserAgents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AgentId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Editable = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotSharp_UserAgents", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BotSharp_Users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FirstName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Phone = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Salt = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Password = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Source = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExternalId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Role = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VerificationCode = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Verified = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotSharp_Users", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BotSharp_States",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Key = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Versioning = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Readonly = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ConversationStateId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotSharp_States", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BotSharp_States_BotSharp_ConversationStates_ConversationStat~",
                        column: x => x.ConversationStateId,
                        principalTable: "BotSharp_ConversationStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BotSharp_StateValues",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Data = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MessageId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ActiveRounds = table.Column<int>(type: "int", nullable: false),
                    DataType = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Source = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdateTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    StateId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
