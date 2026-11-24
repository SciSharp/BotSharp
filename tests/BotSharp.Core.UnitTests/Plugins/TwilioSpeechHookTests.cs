using BotSharp.Plugin.Twilio.Interfaces;
using BotSharp.Plugin.Twilio.Services;
using BotSharp.Plugin.Twilio.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BotSharp.Core.UnitTests.Plugins;

/// <summary>
/// An agent's speech hooks (ITwilioSpeechHook) as TwilioService applies them: one after the other, a failing one changing
/// nothing, and on the welcome greeting, which Twilio speaks from the TwiML and never goes through the session
/// </summary>
public class TwilioSpeechHookTests
{
    private const string AgentId = "relay-agent";

    private sealed class SuffixHook(string suffix, string selfId = AgentId) : ITwilioSpeechHook
    {
        public string SelfId => selfId;

        public string FormatSpeech(string text) => text + suffix;
    }

    private sealed class ThrowingHook : ITwilioSpeechHook
    {
        public string SelfId => AgentId;

        public string FormatSpeech(string text) => throw new InvalidOperationException("The hook is broken.");
    }

    private sealed class NumberHook : ITwilioSpeechHook
    {
        public string SelfId => AgentId;

        public string FormatSpeech(string text) => text.Replace("B977238", "B 9 7 7 2 3 8");
    }

    private static TwilioService ServiceWith(params ITwilioSpeechHook[] hooks)
    {
        var settings = new TwilioSetting
        {
            CallbackHost = "https://host",
            ConversationRelay = new ConversationRelaySetting { Enabled = true }
        };
        var services = new ServiceCollection().AddSingleton(settings);
        foreach (var hook in hooks)
        {
            services.AddSingleton(hook);
        }
        return new TwilioService(settings, services.BuildServiceProvider(), NullLogger<TwilioService>.Instance);
    }

    [Fact]
    public void An_agent_with_no_speech_hook_has_no_formatter_so_nothing_is_held_back()
    {
        Assert.Null(ServiceWith(new SuffixHook("!", selfId: "another-agent")).GetSpeechFormatter(AgentId));
    }

    [Fact]
    public void The_hooks_of_an_agent_are_applied_one_after_the_other()
    {
        var format = ServiceWith(new SuffixHook(" one"), new SuffixHook(" two")).GetSpeechFormatter(AgentId);

        Assert.Equal("Hi one two", format!("Hi"));
    }

    [Fact]
    public void A_speech_hook_that_fails_leaves_the_text_as_it_was_written()
    {
        var format = ServiceWith(new ThrowingHook()).GetSpeechFormatter(AgentId);

        Assert.Equal("Work order B977238.", format!("Work order B977238."));
    }

    [Fact]
    public void The_welcome_greeting_is_said_the_way_the_voice_reads_it()
    {
        var twiml = ServiceWith(new NumberHook()).ReturnConversationRelay(AgentId, "conv-1", "It is B977238.").ToString();

        Assert.Contains("welcomeGreeting=\"It is B 9 7 7 2 3 8.\"", twiml);
        // The session times the silence after it by how long the voice takes to say it
        Assert.Contains($"value=\"{"It is B 9 7 7 2 3 8.".Length}\"", twiml);
    }

    [Fact]
    public void The_welcome_greeting_of_an_agent_with_no_speech_hook_is_said_as_written()
    {
        var twiml = ServiceWith(new NumberHook()).ReturnConversationRelay("another-agent", "conv-1", "It is B977238.").ToString();

        Assert.Contains("welcomeGreeting=\"It is B977238.\"", twiml);
    }
}
