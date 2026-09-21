using BotSharp.Abstraction.Functions.Models;
using BotSharp.Plugin.OpenAI.Providers.Chat;
using OpenAI.Chat;
using Xunit;

namespace BotSharp.Core.UnitTests.Routing;

/// <summary>
/// The streamed counterpart of reading a reply's tool calls. A non-streaming reply hands over a
/// finished list; a streamed one arrives as fragments that have to be reassembled, and that
/// reassembly is the only part of the path with no production evidence behind it -- the streaming
/// branch logs its calls under <c>#if DEBUG</c>, so a Release deployment that uses it shows
/// nothing at all.
/// </summary>
/// <remarks>
/// Why reassembly is guesswork in the first place: <see cref="StreamingChatToolCallUpdate"/> in
/// the OpenAI SDK exposes only FunctionArgumentsUpdate, FunctionName, Kind and ToolCallId. There
/// is no public index, so which call a fragment belongs to has to be inferred -- an update
/// carrying a tool call id different from the one in progress starts a new call, and everything
/// after it belongs to that one.
/// <para>
/// The case these exist for: two calls in one reply. Appending every fragment to a single string,
/// as this did originally, is invisible while a reply asks for one tool and produces a single
/// malformed argument blob the moment it asks for two.
/// </para>
/// </remarks>
public class StreamingToolCallReconstructionTests
{
    /// <summary>
    /// One streamed fragment. Null <paramref name="id"/> means the update continues the call in
    /// progress, which is how the SDK reports every fragment after the first of a call.
    /// </summary>
    private static StreamingChatToolCallUpdate Update(string? id = null, string? name = null, string? args = null)
        => OpenAIChatModelFactory.StreamingChatToolCallUpdate(
            index: 0,
            toolCallId: id!,
            kind: ChatToolCallKind.Function,
            functionName: name!,
            functionArgumentsUpdate: args != null ? BinaryData.FromString(args) : null!);

    [Fact]
    public void Reconstruct_JoinsTheFragmentsOfOneCall()
    {
        var calls = ChatCompletionProvider.ReconstructToolCalls(
        [
            Update(id: "call_1", name: "get_weather", args: "{\"ci"),
            Update(args: "ty\":\"Chi"),
            Update(args: "cago\"}")
        ]);

        var call = Assert.Single(calls);
        Assert.Equal("call_1", call.Id);
        Assert.Equal("get_weather", call.FunctionName);
        Assert.Equal("{\"city\":\"Chicago\"}", call.FunctionArgs);
    }

    /// <summary>
    /// The regression this reassembly exists for. Both calls must come out whole and separate;
    /// the failure it replaced produced one call whose arguments were the two JSON documents
    /// concatenated.
    /// </summary>
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

        Assert.Equal(2, calls.Count);
        Assert.Equal(["call_1", "call_2"], calls.Select(x => x.Id));
        Assert.Equal(["get_weather", "get_time"], calls.Select(x => x.FunctionName));
        Assert.Equal("{\"city\":\"Chicago\"}", calls[0].FunctionArgs);
        Assert.Equal("{\"zone\":\"CST\"}", calls[1].FunctionArgs);
    }

    /// <summary>
    /// A reply really does ask for the same tool more than once -- production shows models doing
    /// it with up to four copies of one name in a single reply. The id is the only thing telling
    /// those apart, so grouping must not fall back to the name.
    /// </summary>
    [Fact]
    public void Reconstruct_SeparatesTwoCallsOfTheSameTool()
    {
        var calls = ChatCompletionProvider.ReconstructToolCalls(
        [
            Update(id: "call_1", name: "process_wo_avoidance", args: "{\"key_words\":\"toilet leak\"}"),
            Update(id: "call_2", name: "process_wo_avoidance", args: "{\"key_words\":\"roof repair\"}")
        ]);

        Assert.Equal(2, calls.Count);
        Assert.Equal(["call_1", "call_2"], calls.Select(x => x.Id));
        Assert.Equal("{\"key_words\":\"toilet leak\"}", calls[0].FunctionArgs);
        Assert.Equal("{\"key_words\":\"roof repair\"}", calls[1].FunctionArgs);
    }

    /// <summary>
    /// A provider that stamps the id on every fragment rather than only on the first must not be
    /// read as opening a new call per fragment.
    /// </summary>
    [Fact]
    public void Reconstruct_DoesNotReopenACallWhenTheIdRepeats()
    {
        var calls = ChatCompletionProvider.ReconstructToolCalls(
        [
            Update(id: "call_1", name: "get_weather", args: "{\"city\":"),
            Update(id: "call_1", args: "\"Chicago\"}")
        ]);

        var call = Assert.Single(calls);
        Assert.Equal("{\"city\":\"Chicago\"}", call.FunctionArgs);
    }

    /// <summary>
    /// The name does not have to arrive with the id. When it comes in a later fragment it still
    /// has to land on the call in progress, or the call is dispatched under an empty name.
    /// </summary>
    [Fact]
    public void Reconstruct_TakesTheNameFromALaterFragment()
    {
        var calls = ChatCompletionProvider.ReconstructToolCalls(
        [
            Update(id: "call_1"),
            Update(name: "get_weather"),
            Update(args: "{}")
        ]);

        var call = Assert.Single(calls);
        Assert.Equal("call_1", call.Id);
        Assert.Equal("get_weather", call.FunctionName);
        Assert.Equal("{}", call.FunctionArgs);
    }

    /// <summary>
    /// A first fragment with no id still opens a call rather than indexing into an empty list.
    /// </summary>
    [Fact]
    public void Reconstruct_OpensACallEvenWhenTheFirstFragmentHasNoId()
    {
        var calls = ChatCompletionProvider.ReconstructToolCalls(
        [
            Update(name: "get_weather", args: "{\"city\":"),
            Update(args: "\"Chicago\"}")
        ]);

        var call = Assert.Single(calls);
        Assert.Equal("get_weather", call.FunctionName);
        Assert.Equal("{\"city\":\"Chicago\"}", call.FunctionArgs);
    }

    /// <summary>
    /// Arguments are never null on the way out: a call the model sent no arguments for has to
    /// serialize as something the provider can put in the request.
    /// </summary>
    [Fact]
    public void Reconstruct_GivesACallWithoutArgumentsAnEmptyString()
    {
        var calls = ChatCompletionProvider.ReconstructToolCalls(
        [
            Update(id: "call_1", name: "list_work_orders")
        ]);

        var call = Assert.Single(calls);
        Assert.Equal(string.Empty, call.FunctionArgs);
    }

    [Fact]
    public void Reconstruct_ReturnsNothingForAnEmptyStream()
    {
        Assert.Empty(ChatCompletionProvider.ReconstructToolCalls([]));
    }

    /// <summary>
    /// Order is the contract the rest of the engine reads: results are appended in the order the
    /// model asked, and a reply that stops the turn part way through keeps the calls before it.
    /// </summary>
    [Fact]
    public void Reconstruct_KeepsTheOrderTheModelSentThemIn()
    {
        var calls = ChatCompletionProvider.ReconstructToolCalls(
        [
            Update(id: "call_1", name: "a", args: "{}"),
            Update(id: "call_2", name: "b", args: "{}"),
            Update(id: "call_3", name: "c", args: "{}")
        ]);

        Assert.Equal(["a", "b", "c"], calls.Select(x => x.FunctionName));
    }

    /// <summary>
    /// What the engine reads afterwards. <see cref="LlmToolCall"/> is the shape the routing side
    /// dispatches on, and a call is only dispatchable with both an id to answer under and a name
    /// to resolve.
    /// </summary>
    [Fact]
    public void Reconstruct_ProducesCallsTheEngineCanDispatch()
    {
        var calls = ChatCompletionProvider.ReconstructToolCalls(
        [
            Update(id: "call_1", name: "get_weather", args: "{\"city\":\"Chicago\"}"),
            Update(id: "call_2", name: "get_time", args: "{\"zone\":\"CST\"}")
        ]);

        Assert.Equal(2, calls.Count);
        Assert.All(calls, call =>
        {
            Assert.False(string.IsNullOrEmpty(call.Id));
            Assert.False(string.IsNullOrEmpty(call.FunctionName));
            Assert.NotNull(call.FunctionArgs);
        });
    }
}
