using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Conversations.Settings;
using BotSharp.Abstraction.Routing;
using BotSharp.Core.Conversations.Hooks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BotSharp.Core.UnitTests.Conversations;

/// <summary>
/// A tool result is worth its tokens to the turn that asked for it and rarely worth them again.
/// This hook is where that is acted on, and these tests pin the two things it must never do:
/// touch what the current turn is about to read, and lose the call itself -- an agent that cannot
/// see it already ran a function runs it again.
/// </summary>
public class ToolResultTrimHookTests
{
    private const string OldTurn = "turn-1";
    private const string MiddleTurn = "turn-2";
    private const string RecentTurn = "turn-3";
    private const string CurrentTurn = "turn-4";

    private static ToolResultTrimHook BuildHook(ConversationSetting settings, string currentMessageId)
    {
        var context = new Mock<IRoutingContext>();
        context.SetupGet(x => x.MessageId).Returns(currentMessageId);

        var services = new ServiceCollection();
        services.AddSingleton(context.Object);

        return new ToolResultTrimHook(
            services.BuildServiceProvider(),
            settings,
            NullLogger<ToolResultTrimHook>.Instance);
    }

    private static ConversationSetting Settings(bool enable = true, int keepTurns = 2, int maxLength = 500)
        => new()
        {
            ToolResultTrim = new ToolResultTrimSetting
            {
                Enable = enable,
                KeepTurns = keepTurns,
                MaxLength = maxLength
            }
        };

    private static RoleDialogModel Tool(string messageId, string content, string function = "read_work_order")
        => new(AgentRole.Function, content)
        {
            MessageId = messageId,
            FunctionName = function,
            ToolCallId = $"call_{messageId}",
            FunctionArgs = "{\"wo_num\":\"A123\"}"
        };

    private static string Rendered(int length)
        => "WO Num: A1234567\r\n" + new string('x', length - 18);

    private static string Json(int length)
        => "{\"wo_num\":\"A1234567\",\"pad\":\"" + new string('x', length - 30) + "\"}";

    [Fact]
    public async Task Leaves_the_turn_that_is_running_untouched()
    {
        var dialogs = new List<RoleDialogModel>
        {
            Tool(OldTurn, Rendered(4000)),
            Tool(MiddleTurn, Rendered(4000)),
            Tool(RecentTurn, Rendered(4000)),
            Tool(CurrentTurn, Rendered(4000))
        };

        // keepTurns 0, so only "this is the turn in flight" can protect the last one.
        await BuildHook(Settings(keepTurns: 0), CurrentTurn).OnDialogsLoaded(dialogs);

        Assert.Equal(4000, dialogs[3].Content.Length);
        Assert.All(dialogs.Take(3), x => Assert.True(x.Content.Length < 4000));
    }

    [Fact]
    public async Task Keeps_the_most_recent_turns_whole_and_shortens_what_is_older()
    {
        var dialogs = new List<RoleDialogModel>
        {
            new(AgentRole.User, "how is my work order?") { MessageId = OldTurn },
            Tool(OldTurn, Rendered(4000)),
            Tool(MiddleTurn, Rendered(4000)),
            Tool(RecentTurn, Rendered(4000))
        };

        await BuildHook(Settings(keepTurns: 2), CurrentTurn).OnDialogsLoaded(dialogs);

        Assert.True(dialogs[1].Content.Length < 4000);
        Assert.Equal(4000, dialogs[2].Content.Length);
        Assert.Equal(4000, dialogs[3].Content.Length);
    }

    [Fact]
    public async Task Keeps_the_call_even_when_the_result_goes()
    {
        var dialogs = new List<RoleDialogModel> { Tool(OldTurn, Json(4000)) };

        await BuildHook(Settings(keepTurns: 0), CurrentTurn).OnDialogsLoaded(dialogs);

        var stored = dialogs[0];
        Assert.Equal(AgentRole.Function, stored.Role);
        Assert.Equal("read_work_order", stored.FunctionName);
        Assert.Equal($"call_{OldTurn}", stored.ToolCallId);
        Assert.Equal("{\"wo_num\":\"A123\"}", stored.FunctionArgs);
    }

    [Fact]
    public async Task Replaces_a_structured_result_rather_than_cutting_it_in_half()
    {
        var dialogs = new List<RoleDialogModel> { Tool(OldTurn, Json(4000)) };

        await BuildHook(Settings(keepTurns: 0), CurrentTurn).OnDialogsLoaded(dialogs);

        // Half a JSON document reads as a whole one to a model that cannot see where it was cut.
        Assert.DoesNotContain("{\"wo_num\"", dialogs[0].Content);
        Assert.Equal("[4000 chars omitted; call again for detail]", dialogs[0].Content);
    }

    [Fact]
    public async Task Keeps_the_head_of_a_rendered_result_and_says_what_was_dropped()
    {
        var dialogs = new List<RoleDialogModel> { Tool(OldTurn, Rendered(4000)) };

        await BuildHook(Settings(keepTurns: 0, maxLength: 500), CurrentTurn).OnDialogsLoaded(dialogs);

        Assert.StartsWith("WO Num: A1234567", dialogs[0].Content);
        Assert.Contains("3500 chars omitted", dialogs[0].Content);
    }

    [Fact]
    public async Task Keeps_the_sentence_that_introduces_a_document_and_drops_the_document()
    {
        // What an MCP server answers with is several text blocks joined together, so a document
        // can sit behind a line of prose. The prose is what a later turn can still use.
        var dialogs = new List<RoleDialogModel>
        {
            Tool(OldTurn, "Found 3 work orders:\r\n" + Json(4000))
        };

        await BuildHook(Settings(keepTurns: 0, maxLength: 500), CurrentTurn).OnDialogsLoaded(dialogs);

        Assert.StartsWith("Found 3 work orders:", dialogs[0].Content);
        Assert.DoesNotContain("\"wo_num\"", dialogs[0].Content);
        Assert.Contains("chars omitted", dialogs[0].Content);
    }

    [Fact]
    public async Task Keeps_prose_whose_document_begins_past_the_cut()
    {
        var dialogs = new List<RoleDialogModel>
        {
            Tool(OldTurn, Rendered(900) + Json(3000))
        };

        await BuildHook(Settings(keepTurns: 0, maxLength: 500), CurrentTurn).OnDialogsLoaded(dialogs);

        Assert.StartsWith("WO Num: A1234567", dialogs[0].Content);
        Assert.DoesNotContain("\"wo_num\"", dialogs[0].Content);
    }

    [Theory]
    [InlineData("{\"wo_num\": \"A123\", ")]          // object
    [InlineData("[{\"wo_num\": \"A123\"}, ")]        // array of objects
    [InlineData("[\"A123\", \"A456\", ")]            // array of strings
    [InlineData("[1, 2, 3, 4, 5, 6, 7, 8, 9, 10, ")]   // array of numbers
    public async Task Treats_an_opening_value_as_structured(string opening)
    {
        var dialogs = new List<RoleDialogModel> { Tool(OldTurn, opening + new string('x', 4000)) };

        await BuildHook(Settings(keepTurns: 0, maxLength: 500), CurrentTurn).OnDialogsLoaded(dialogs);

        Assert.Matches(@"^\[\d+ chars omitted; call again for detail\]$", dialogs[0].Content);
    }

    [Fact]
    public async Task Does_not_mistake_a_brace_in_a_sentence_for_a_document()
    {
        // Prose is allowed to contain a brace. Replacing the whole result over one would throw
        // away a head that was perfectly safe to keep.
        var dialogs = new List<RoleDialogModel>
        {
            Tool(OldTurn, "Use the {name} placeholder in the template. " + new string('x', 4000))
        };

        await BuildHook(Settings(keepTurns: 0, maxLength: 500), CurrentTurn).OnDialogsLoaded(dialogs);

        Assert.StartsWith("Use the {name} placeholder", dialogs[0].Content);
        Assert.Contains("chars omitted", dialogs[0].Content);
    }

    [Fact]
    public async Task Leaves_a_short_result_alone()
    {
        var dialogs = new List<RoleDialogModel> { Tool(OldTurn, Rendered(400)) };

        await BuildHook(Settings(keepTurns: 0), CurrentTurn).OnDialogsLoaded(dialogs);

        Assert.Equal(400, dialogs[0].Content.Length);
    }

    [Fact]
    public async Task Stands_in_for_a_result_the_assistant_message_repeats()
    {
        // A function that writes the user-facing reply itself has its text stored twice. Short
        // enough to pass the length cap, and shown to the model as a tool result echoed word for
        // word by the assistant -- which is what the model then learns to do.
        const string reply = "Are you creating a duplicate work order or not?";
        var dialogs = new List<RoleDialogModel>
        {
            Tool(OldTurn, reply, function: "check_prerequisites"),
            new(AgentRole.Assistant, reply) { MessageId = OldTurn }
        };

        await BuildHook(Settings(keepTurns: 0), CurrentTurn).OnDialogsLoaded(dialogs);

        Assert.Equal("[replied to the user]", dialogs[0].Content);
        Assert.Equal("check_prerequisites", dialogs[0].FunctionName);
        Assert.Equal(reply, dialogs[1].Content);
    }

    [Fact]
    public async Task Leaves_a_result_the_assistant_only_paraphrased()
    {
        var dialogs = new List<RoleDialogModel>
        {
            Tool(OldTurn, "Got location id 750229, resident id 1673151, continue current process."),
            new(AgentRole.Assistant, "Got it. Can you tell me about the issue?") { MessageId = OldTurn }
        };

        await BuildHook(Settings(keepTurns: 0), CurrentTurn).OnDialogsLoaded(dialogs);

        Assert.StartsWith("Got location id 750229", dialogs[0].Content);
    }

    [Fact]
    public async Task Stands_in_even_for_a_turn_that_keeps_its_results()
    {
        // Shortening spares the recent turns because their detail may still be wanted. A repeat
        // has no detail to spare: the assistant message beside it says the same thing.
        const string reply = "Are you ready for some questions?";
        var dialogs = new List<RoleDialogModel>
        {
            Tool(RecentTurn, reply),
            new(AgentRole.Assistant, reply) { MessageId = RecentTurn }
        };

        await BuildHook(Settings(keepTurns: 2), CurrentTurn).OnDialogsLoaded(dialogs);

        Assert.Equal("[replied to the user]", dialogs[0].Content);
    }

    [Fact]
    public async Task Does_not_match_the_same_text_from_another_turn()
    {
        const string reply = "Are you creating a duplicate work order or not?";
        var dialogs = new List<RoleDialogModel>
        {
            Tool(OldTurn, reply),
            new(AgentRole.Assistant, reply) { MessageId = MiddleTurn }
        };

        await BuildHook(Settings(keepTurns: 0), CurrentTurn).OnDialogsLoaded(dialogs);

        Assert.Equal(reply, dialogs[0].Content);
    }

    [Fact]
    public async Task Does_nothing_at_all_when_switched_off()
    {
        var dialogs = new List<RoleDialogModel> { Tool(OldTurn, Json(4000)) };

        await BuildHook(Settings(enable: false, keepTurns: 0), CurrentTurn).OnDialogsLoaded(dialogs);

        Assert.Equal(4000, dialogs[0].Content.Length);
    }

    [Fact]
    public async Task Leaves_every_role_but_function_alone()
    {
        var dialogs = new List<RoleDialogModel>
        {
            new(AgentRole.User, Rendered(4000)) { MessageId = OldTurn },
            new(AgentRole.Assistant, Rendered(4000)) { MessageId = OldTurn }
        };

        await BuildHook(Settings(keepTurns: 0), CurrentTurn).OnDialogsLoaded(dialogs);

        Assert.All(dialogs, x => Assert.Equal(4000, x.Content.Length));
    }
}
