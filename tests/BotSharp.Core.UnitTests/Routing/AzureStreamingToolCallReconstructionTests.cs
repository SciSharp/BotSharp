using OpenAI.Chat;
using Xunit;
using ChatCompletionProvider = BotSharp.Plugin.AzureOpenAI.Providers.Chat.ChatCompletionProvider;

namespace BotSharp.Core.UnitTests.Routing;

/// <summary>
/// The Azure OpenAI counterpart of <see cref="StreamingToolCallReconstructionTests"/>.
/// </summary>
public class AzureStreamingToolCallReconstructionTests
{
    private static StreamingChatToolCallUpdate Update(string? id = null, string? name = null, string? args = null)
        => OpenAIChatModelFactory.StreamingChatToolCallUpdate(
            index: 0,
            toolCallId: id!,
            kind: ChatToolCallKind.Function,
            functionName: name!,
            functionArgumentsUpdate: args != null ? BinaryData.FromString(args) : null!);

    [Fact]
    public void Reconstruct_KeepsTwoCallsArgumentsApart()
    {
        var calls = ChatCompletionProvider.ReconstructToolCalls(
        [
            Update(id: "call_1", name: "get_weather", args: "{\"city\":"),
            Update(args: "\"Chicago\"}"),
            Update(id: "call_2", name: "get_time", args: "{\"zone\":"),
            Update(args: "\"CST\"}")
        ]);

        Assert.Collection(calls,
            call =>
            {
                Assert.Equal("call_1", call.Id);
                Assert.Equal("get_weather", call.FunctionName);
                Assert.Equal("{\"city\":\"Chicago\"}", call.FunctionArgs);
            },
            call =>
            {
                Assert.Equal("call_2", call.Id);
                Assert.Equal("get_time", call.FunctionName);
                Assert.Equal("{\"zone\":\"CST\"}", call.FunctionArgs);
            });
    }
}
