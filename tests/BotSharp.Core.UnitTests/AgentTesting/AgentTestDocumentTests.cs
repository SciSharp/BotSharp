using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using BotSharp.Plugin.AgentTesting.Models;
using Xunit;

namespace BotSharp.Core.UnitTests.AgentTesting;

/// <summary>
/// A case document has to round-trip through BSON losslessly. Not ceremony: a mock's fake return and
/// an assertion's expected value are arbitrary user-supplied strings, JSON fragments included, and
/// if the Mongo serialiser swallows or reshapes one of those fields the symptom is "the case looks
/// different after saving and reopening it", not an exception.
/// </summary>
public class AgentTestDocumentTests
{
    [Fact]
    public void A_case_round_trips_through_bson_without_losing_nested_content()
    {
        var original = new AgentTestCase
        {
            SuiteId = "suite-1",
            // Deliberately non-ASCII: this test exists to prove the Mongo serialiser does not
            // swallow or reshape user-supplied text, and an all-ASCII fixture would not show that.
            Name = "Tenant reports a leaking sink — ünïcode ✓",
            // UnmockedToolPolicies only ever has one member (Block) as of the P1 fix wave that
            // rejected Passthrough -- this round trip only cares that whatever string is stored
            // survives BSON serialization unchanged, so any non-default literal proves the point.
            UnmockedToolPolicy = "SomeFutureNonDefaultPolicy",
            SourceConversationId = "conv-9",
            InitialStates = [new TestState { Key = "user_authenticated", Value = "true" }],
            Mocks =
            [
                new TestToolMock
                {
                    FunctionName = "get_work_order",
                    ArgsMatchJson = """{"woNum":"B9897413"}""",
                    CallIndex = 1,
                    ResultContent = """{"status":"Open","trade":"Plumbing"}""",
                    StopCompletion = true,
                    StateWrites = [new TestState { Key = "wo_id", Value = "123", ActiveRounds = 5 }]
                }
            ],
            Turns =
            [
                new TestTurn
                {
                    Index = 0,
                    UserMessage = "my sink is leaking",
                    Assertions = [new TestAssertion { Type = "toolCalled", Target = "get_work_order", Fatal = true }]
                }
            ],
            Assertions = [new TestAssertion { Type = "stateEquals", Target = "wo_id", Expected = "123" }]
        };

        var bson = original.ToBson();
        var restored = BsonSerializer.Deserialize<AgentTestCase>(bson);

        Assert.Equal("Tenant reports a leaking sink — ünïcode ✓", restored.Name);
        Assert.Equal("SomeFutureNonDefaultPolicy", restored.UnmockedToolPolicy);
        var mock = Assert.Single(restored.Mocks);
        Assert.Equal("""{"woNum":"B9897413"}""", mock.ArgsMatchJson);
        Assert.Equal(1, mock.CallIndex);
        Assert.True(mock.StopCompletion);
        Assert.Equal("123", Assert.Single(mock.StateWrites!).Value);
        var turn = Assert.Single(restored.Turns);
        Assert.True(Assert.Single(turn.Assertions).Fatal);
        Assert.Equal("stateEquals", Assert.Single(restored.Assertions).Type);
    }

    [Fact]
    public void Optional_fields_survive_being_absent()
    {
        // Hand-written and AI-generated cases routinely fill in only some fields, and a missing one
        // must not fail deserialisation.
        var minimal = new AgentTestCase { SuiteId = "s", Name = "n" };

        var restored = BsonSerializer.Deserialize<AgentTestCase>(minimal.ToBson());

        Assert.Empty(restored.Turns);
        Assert.Empty(restored.Mocks);
        Assert.Null(restored.SourceConversationId);
        Assert.Equal(UnmockedToolPolicies.Block, restored.UnmockedToolPolicy);   // must default to Block
    }
}
