using BotSharp.Abstraction.Functions.Models;
using BotSharp.Plugin.OpenAI.Providers.Chat;
using Xunit;

namespace BotSharp.Core.UnitTests.Routing;

/// <summary>
/// What the OpenAI provider logs when a reply runs into the output limit. The log has to show what the model was doing
/// (which tools, how far it got) without carrying what it wrote: tool arguments and replies can hold the user's
/// personal data, and the line is a warning that goes to the shared log store.
/// </summary>
public class OutputLimitLogTests
{
    [Fact]
    public void Tool_calls_are_logged_by_name_and_argument_length_only()
    {
        var calls = new List<LlmToolCall>
        {
            new("call_1", "create_request", "{\"contact_phone\":\"675-555-0118\",\"note\":\"gate code 4411\"}"),
            new("call_2", "get_result", null)
        };

        var logged = ChatCompletionProvider.DescribeToolCalls(calls);

        Assert.Equal("create_request(56 chars), get_result(0 chars)", logged);
        Assert.DoesNotContain("675-555-0118", logged);
    }

    [Fact]
    public void A_reply_is_logged_by_its_length_and_its_first_200_characters()
    {
        var reply = new string('a', 200) + "675-555-0118";

        var logged = ChatCompletionProvider.DescribeContent(reply);

        Assert.Equal($"212 chars: {new string('a', 200)}...", logged);
        Assert.DoesNotContain("675-555-0118", logged);
        Assert.Equal("5 chars: Hello", ChatCompletionProvider.DescribeContent("Hello"));
        Assert.Equal("(none)", ChatCompletionProvider.DescribeContent(null));
        Assert.Equal("(none)", ChatCompletionProvider.DescribeContent(string.Empty));
    }
}
