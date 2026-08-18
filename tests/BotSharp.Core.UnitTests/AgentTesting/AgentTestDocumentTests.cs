using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using BotSharp.Plugin.AgentTesting.Models;
using Xunit;

namespace BotSharp.Core.UnitTests.AgentTesting;

/// <summary>
/// 用例文档要能无损往返 BSON。这不是形式主义：mock 的假返回、断言的期望值都是用户输入的
/// 任意字符串（含 JSON 片段），一旦某个字段被 Mongo 序列化器吞掉或改形，症状是"用例保存后
/// 再打开变了个样"，而不是抛异常。
/// </summary>
public class AgentTestDocumentTests
{
    [Fact]
    public void A_case_round_trips_through_bson_without_losing_nested_content()
    {
        var original = new AgentTestCase
        {
            SuiteId = "suite-1",
            Name = "租户报修水槽漏水",
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

        Assert.Equal("租户报修水槽漏水", restored.Name);
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
        // 手写/AI 生成的用例经常只填一部分字段，缺字段不能反序列化失败。
        var minimal = new AgentTestCase { SuiteId = "s", Name = "n" };

        var restored = BsonSerializer.Deserialize<AgentTestCase>(minimal.ToBson());

        Assert.Empty(restored.Turns);
        Assert.Empty(restored.Mocks);
        Assert.Null(restored.SourceConversationId);
        Assert.Equal(UnmockedToolPolicies.Block, restored.UnmockedToolPolicy);   // 默认必须是阻断
    }
}
