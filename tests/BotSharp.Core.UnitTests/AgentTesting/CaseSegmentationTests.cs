using System;
using System.Collections.Generic;
using System.Linq;
using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Plugin.AgentTesting.Services;
using BotSharp.Plugin.AgentTesting.Models;
using Xunit;

namespace BotSharp.Core.UnitTests.AgentTesting;

/// <summary>
/// AI extraction is allowed to decide exactly two things: where to cut and what to name each piece.
/// These tests pin both sides of that boundary -- how far the parser distrusts the model's output,
/// and the corrections slicing needs that BuildDraft cannot see for itself.
/// </summary>
public class CaseSegmentationTests
{
    // ---- LlmCaseSegmenter.Parse: extend the model's answer no trust -------------------------

    [Fact]
    public void Parse_accepts_a_well_formed_contiguous_split()
    {
        var segments = LlmCaseSegmenter.Parse(
            """{"segments":[{"name":"ETA","firstTurn":0,"lastTurn":1},{"name":"Reschedule","firstTurn":2,"lastTurn":2}]}""",
            turnCount: 3);

        Assert.Equal(2, segments.Count);
        Assert.Equal(new CaseSegment("ETA", 0, 1), segments[0]);
        Assert.Equal(new CaseSegment("Reschedule", 2, 2), segments[1]);
    }

    [Fact]
    public void Parse_tolerates_a_code_fence_and_surrounding_prose()
    {
        // The commonest and most harmless way for a model to disobey "JSON only". Failing the whole
        // extraction over a ``` would just make the feature look flaky.
        var segments = LlmCaseSegmenter.Parse(
            "Sure! Here you go:\n```json\n{\"segments\":[{\"name\":\"All\",\"firstTurn\":0,\"lastTurn\":0}]}\n```",
            turnCount: 1);

        Assert.Single(segments);
    }

    [Theory]
    // A gap: turn 1 belongs to no case, so it silently disappears from the test set.
    [InlineData("""{"segments":[{"name":"a","firstTurn":0,"lastTurn":0},{"name":"b","firstTurn":2,"lastTurn":2}]}""", 3)]
    // An overlap: turn 1 lands in two cases.
    [InlineData("""{"segments":[{"name":"a","firstTurn":0,"lastTurn":1},{"name":"b","firstTurn":1,"lastTurn":2}]}""", 3)]
    // Out of range.
    [InlineData("""{"segments":[{"name":"a","firstTurn":0,"lastTurn":9}]}""", 3)]
    // Trailing turns left uncovered.
    [InlineData("""{"segments":[{"name":"a","firstTurn":0,"lastTurn":0}]}""", 3)]
    // Not JSON at all.
    [InlineData("I could not determine the segments.", 3)]
    // Structurally fine, semantically empty.
    [InlineData("""{"segments":[]}""", 3)]
    public void Parse_rejects_anything_it_cannot_fully_verify(string raw, int turnCount)
    {
        // Half-right segmentation is the dangerous outcome: the drafts look plausible in the UI
        // (real names, real turns) and only misbehave when someone runs them.
        Assert.Throws<InvalidOperationException>(() => LlmCaseSegmenter.Parse(raw, turnCount));
    }

    [Fact]
    public void Parse_falls_back_to_a_positional_name_when_the_model_leaves_it_blank()
    {
        var segments = LlmCaseSegmenter.Parse(
            """{"segments":[{"name":"   ","firstTurn":0,"lastTurn":1}]}""", turnCount: 2);

        Assert.Equal("Turns 0-1", segments[0].Name);
    }

    // ---- AgentTestRecorder.BuildDrafts: the two corrections slicing needs -------------------

    private static List<RecordedDialog> TwoScenarioConversation() =>
    [
        new() { Role = AgentRole.User, Content = "where is my tech", MessageId = "m0" },
        new() { Role = AgentRole.Assistant, FunctionName = "get_eta", FunctionArgs = """{"wo_num":"B1"}""" },
        new() { Role = AgentRole.Function, FunctionName = "get_eta", Content = """{"eta":"2pm"}""" },
        new() { Role = AgentRole.User, Content = "reschedule it", MessageId = "m1" },
        new() { Role = AgentRole.Assistant, FunctionName = "reschedule", FunctionArgs = """{"wo_num":"B1"}""" },
        new() { Role = AgentRole.Function, FunctionName = "reschedule", Content = """{"ok":true}""" }
    ];

    private static List<RecordedState> TwoScenarioStates() =>
    [
        new()
        {
            Key = "wo_num",
            Values =
            [
                new RecordedStateValue { MessageId = null, Data = "B1", ActiveRounds = -1 },
            ]
        },
        new()
        {
            Key = "stage",
            Values =
            [
                new RecordedStateValue { MessageId = "m0", Data = "eta-shown", ActiveRounds = -1 },
                new RecordedStateValue { MessageId = "m1", Data = "rescheduled", ActiveRounds = -1 }
            ]
        }
    ];

    [Fact]
    public void BuildDrafts_gives_each_segment_only_its_own_turns_and_mocks()
    {
        var drafts = AgentTestRecorder.BuildDrafts(
            "suite-1", "conv-1", TwoScenarioConversation(), TwoScenarioStates(),
            [new CaseSegment("ETA", 0, 0), new CaseSegment("Reschedule", 1, 1)]);

        Assert.Equal(2, drafts.Count);
        Assert.Equal("ETA", drafts[0].Name);
        Assert.Equal("where is my tech", Assert.Single(drafts[0].Turns).UserMessage);
        Assert.Equal("get_eta", Assert.Single(drafts[0].Mocks).FunctionName);

        Assert.Equal("reschedule it", Assert.Single(drafts[1].Turns).UserMessage);
        Assert.Equal("reschedule", Assert.Single(drafts[1].Mocks).FunctionName);

        // The mock payload is the real recorded return value, not something a model wrote.
        Assert.Equal("""{"ok":true}""", drafts[1].Mocks[0].ResultContent);

        // Every draft still lands disabled, exactly like the single-case recorder.
        Assert.All(drafts, d => Assert.False(d.Enabled));
        Assert.All(drafts, d => Assert.Equal("conv-1", d.SourceConversationId));
    }

    [Fact]
    public void BuildDrafts_carries_earlier_state_into_a_segment_that_does_not_start_at_turn_zero()
    {
        var drafts = AgentTestRecorder.BuildDrafts(
            "suite-1", "conv-1", TwoScenarioConversation(), TwoScenarioStates(),
            [new CaseSegment("ETA", 0, 0), new CaseSegment("Reschedule", 1, 1)]);

        // Segment 0 starts the conversation, so only the seeded value is initial state.
        var first = drafts[0].InitialStates;
        Assert.Equal("B1", Assert.Single(first, s => s.Key == "wo_num").Value);
        Assert.DoesNotContain(first, s => s.Key == "stage");

        // Segment 1 starts mid-conversation. Without the carry-in it would begin with `stage`
        // unset and run a path the recording never took.
        var second = drafts[1].InitialStates;
        Assert.Equal("B1", Assert.Single(second, s => s.Key == "wo_num").Value);
        Assert.Equal("eta-shown", Assert.Single(second, s => s.Key == "stage").Value);
    }

    [Fact]
    public void BuildDrafts_asserts_the_state_value_its_own_segment_reaches_not_the_conversations_final_one()
    {
        var drafts = AgentTestRecorder.BuildDrafts(
            "suite-1", "conv-1", TwoScenarioConversation(), TwoScenarioStates(),
            [new CaseSegment("ETA", 0, 0), new CaseSegment("Reschedule", 1, 1)]);

        // BuildDraft's own step 5b would put the whole conversation's LAST value ("rescheduled")
        // on both drafts -- guaranteeing that the first one fails on every run, because it stops
        // before that write ever happens.
        var firstStage = Assert.Single(drafts[0].Assertions, a => a.Target == "stage");
        Assert.Equal("eta-shown", firstStage.Expected);

        var secondStage = Assert.Single(drafts[1].Assertions, a => a.Target == "stage");
        Assert.Equal("rescheduled", secondStage.Expected);
    }

    [Fact]
    public void ToSegmentableTurns_exposes_tool_names_but_not_their_arguments_or_results()
    {
        // The egress boundary: a change here silently starts shipping work order payloads --
        // addresses, phone numbers -- to a model vendor.
        var turns = AgentTestRecorder.ToSegmentableTurns(TwoScenarioConversation());

        Assert.Equal(2, turns.Count);
        Assert.Equal(["get_eta"], turns[0].ToolNames);
        Assert.Equal(["reschedule"], turns[1].ToolNames);

        var serialised = string.Join("|", turns.Select(t => t.UserMessage + string.Join(",", t.ToolNames)));
        Assert.DoesNotContain("B1", serialised);
        Assert.DoesNotContain("2pm", serialised);
    }
}
