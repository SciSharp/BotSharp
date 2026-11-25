using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using FloxiChatCompletionProvider = BotSharp.Plugin.FloxiAI.Providers.Chat.ChatCompletionProvider;

namespace BotSharp.LLM.Tests;

public class FloxiStreamToolCallTests
{
    [Fact]
    public void InterleavedParallelCallsDoNotMixArguments()
    {
        var aggregate = Collect(
            """{"index":0,"id":"call_a","function":{"name":"search","arguments":"{\"q\":\"A"}}""",
            """{"index":1,"id":"call_b","function":{"name":"lookup","arguments":"{\"id\":7}"}}""",
            """{"index":0,"function":{"arguments":"BC\"}"}}""");

        Read(aggregate, "ToolCallId").ShouldBe("call_a");
        Read(aggregate, "ToolCallName").ShouldBe("search");
        Read(aggregate, "ToolCallArguments").ShouldBe("""{"q":"ABC"}""");
    }

    [Fact]
    public void FollowedCallIsPickedByIndexWhenOneChunkCarriesSeveral()
    {
        var aggregate = Collect(
            """{"index":0,"id":"call_a","function":{"name":"search","arguments":"{\"q\":\"A"}}""",
            """{"index":1,"id":"call_b","function":{"name":"lookup","arguments":"{}"}},{"index":0,"function":{"arguments":"BC\"}"}}""");

        Read(aggregate, "ToolCallArguments").ShouldBe("""{"q":"ABC"}""");
    }

    private static object Collect(params string[] toolCallDeltas)
    {
        var providerType = typeof(FloxiChatCompletionProvider);
        var provider = new FloxiChatCompletionProvider(null!, NullLogger<FloxiChatCompletionProvider>.Instance);
        var aggregate = Activator.CreateInstance(
            providerType.GetNestedType("FloxiStreamAggregate", BindingFlags.NonPublic)!, nonPublic: true)!;
        var collect = providerType.GetMethod("CollectChunk", BindingFlags.NonPublic | BindingFlags.Instance)!;

        foreach (var delta in toolCallDeltas)
        {
            var chunk = $$$"""{"choices":[{"delta":{"tool_calls":[{{{delta}}}]}}]}""";
            collect.Invoke(provider, [chunk, aggregate]);
        }

        return aggregate;
    }

    private static string? Read(object aggregate, string property)
    {
        var value = aggregate.GetType().GetProperty(property)!.GetValue(aggregate);
        return value is StringBuilder builder ? builder.ToString() : (string?)value;
    }
}
