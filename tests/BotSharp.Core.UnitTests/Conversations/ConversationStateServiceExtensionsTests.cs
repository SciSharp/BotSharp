using System.Globalization;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Enums;
using BotSharp.Abstraction.Infrastructures.Enums;
using Moq;
using Xunit;

namespace BotSharp.Core.UnitTests.Conversations;

public class ConversationStateServiceExtensionsTests
{
    public enum Color { Red, Green }

    public class Payload
    {
        public string Name { get; set; } = "";
        public int Count { get; set; }
    }

    /// <summary>
    /// Mirrors ConversationStateService.GetState: a missing key yields the caller's defaultValue.
    /// </summary>
    private static IConversationStateService States(params (string Key, string Value)[] entries)
    {
        var data = entries.ToDictionary(e => e.Key, e => e.Value);
        var mock = new Mock<IConversationStateService>();
        mock.Setup(s => s.GetState(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string name, string defaultValue) => data.TryGetValue(name, out var v) ? v : defaultValue);
        return mock.Object;
    }

    #region GetState<T>

    [Fact]
    public void GetState_Int_ParsesValue()
    {
        Assert.Equal(42, States(("k", "42")).GetState<int>("k"));
    }

    [Fact]
    public void GetState_Int_MissingKey_ReturnsDefault()
    {
        Assert.Equal(0, States().GetState<int>("k"));
        Assert.Equal(7, States().GetState("k", 7));
    }

    [Fact]
    public void GetState_Int_InvalidValue_ReturnsDefault()
    {
        Assert.Equal(7, States(("k", "abc")).GetState("k", 7));
    }

    [Fact]
    public void GetState_NullableInt_MissingKey_ReturnsNull()
    {
        Assert.Null(States().GetState<int?>("k"));
    }

    [Fact]
    public void GetState_NullableInt_ParsesValue()
    {
        Assert.Equal(5, States(("k", "5")).GetState<int?>("k"));
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("True", true)]
    [InlineData("false", false)]
    public void GetState_Bool_ParsesValue(string raw, bool expected)
    {
        Assert.Equal(expected, States(("k", raw)).GetState<bool>("k"));
    }

    [Fact]
    public void GetState_String_ReturnsRawValue()
    {
        Assert.Equal("hello", States(("k", "hello")).GetState<string>("k"));
    }

    [Fact]
    public void GetState_Decimal_UsesInvariantCultureRegardlessOfCurrentCulture()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            // fr-FR uses ',' as decimal separator; parsing must still treat '.' as the separator.
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            Assert.Equal(1.5m, States(("k", "1.5")).GetState<decimal>("k"));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void GetState_DateTime_PreservesUtcKind()
    {
        var result = States(("k", "2026-09-29T10:00:00Z")).GetState<DateTime>("k");

        Assert.Equal(new DateTime(2026, 9, 29, 10, 0, 0, DateTimeKind.Utc), result);
        Assert.Equal(DateTimeKind.Utc, result.Kind);
    }

    [Fact]
    public void GetState_NullableDateTime_MissingKey_ReturnsNull()
    {
        Assert.Null(States().GetState<DateTime?>("k"));
    }

    [Theory]
    [InlineData("Green")]
    [InlineData("green")]
    [InlineData("1")]
    public void GetState_Enum_ParsesNameIgnoringCaseOrNumber(string raw)
    {
        Assert.Equal(Color.Green, States(("k", raw)).GetState<Color>("k"));
    }

    [Fact]
    public void GetState_Enum_InvalidValue_ReturnsDefault()
    {
        Assert.Equal(Color.Green, States(("k", "Blue")).GetState("k", Color.Green));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GetState_Object_DeserializesJson(bool useNewtonsoftJson)
    {
        var states = States(("k", "{\"name\":\"a\",\"count\":3}"));

        var result = states.GetState<Payload>("k", useNewtonsoftJson: useNewtonsoftJson);

        Assert.NotNull(result);
        Assert.Equal("a", result.Name);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void GetState_Object_JsonNull_ReturnsDefault()
    {
        var fallback = new Payload { Name = "fallback" };

        Assert.Same(fallback, States(("k", "null")).GetState("k", fallback));
    }

    [Fact]
    public void GetState_Object_InvalidJson_ReturnsDefault()
    {
        Assert.Null(States(("k", "{not json")).GetState<Payload>("k"));
    }

    [Fact]
    public void GetState_Object_MissingKey_ReturnsDefault()
    {
        var fallback = new Payload();

        Assert.Same(fallback, States().GetState("k", fallback));
    }

    [Fact]
    public void GetState_ListOfInt_DeserializesJsonArray()
    {
        Assert.Equal(new List<int> { 1, 2 }, States(("k", "[1,2]")).GetState<List<int>>("k"));
    }

    #endregion

    #region Equal / IsTrue / IsNullOrEmpty

    [Fact]
    public void Equal_And_NotEqual()
    {
        var states = States(("k", "v"));

        Assert.True(states.Equal("k", "v"));
        Assert.False(states.NotEqual("k", "v"));
        Assert.False(states.Equal("k", "other"));
        Assert.True(states.NotEqual("k", "other"));
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("TRUE", true)]
    [InlineData("false", false)]
    [InlineData("yes", false)]
    [InlineData("1", false)]
    public void IsTrue_And_IsFalse(string raw, bool expected)
    {
        var states = States(("k", raw));

        Assert.Equal(expected, states.IsTrue("k"));
        Assert.Equal(!expected, states.IsFalse("k"));
    }

    [Fact]
    public void IsTrue_MissingKey_ReturnsFalse()
    {
        Assert.False(States().IsTrue("k"));
        Assert.True(States().IsFalse("k"));
    }

    [Fact]
    public void IsNullOrEmpty_And_IsNotNullOrEmpty()
    {
        var states = States(("empty", ""), ("set", "x"));

        Assert.True(states.IsNullOrEmpty("missing"));
        Assert.True(states.IsNullOrEmpty("empty"));
        Assert.False(states.IsNullOrEmpty("set"));
        Assert.True(states.IsNotNullOrEmpty("set"));
    }

    #endregion

    #region Channels

    [Theory]
    [InlineData(ConversationChannel.Phone, true, false, false)]
    [InlineData(ConversationChannel.Email, false, true, false)]
    [InlineData(ConversationChannel.OpenAPI, false, false, true)]
    [InlineData("webchat", false, false, false)]
    public void ChannelChecks(string channel, bool isPhone, bool isEmail, bool isOpenApi)
    {
        var states = States((StateConst.CHANNEL, channel));

        Assert.Equal(isPhone, states.IsPhoneChannel());
        Assert.Equal(isEmail, states.IsEmailChannel());
        Assert.Equal(isOpenApi, states.IsOpenAPIChannel());
    }

    #endregion
}
