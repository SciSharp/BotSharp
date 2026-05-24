using System.Reflection;
using OpenSandbox.Config;

namespace BotSharp.Plugin.CodeAct.UnitTests.OpenSandbox;

public class OpenSandboxHttpCodeClientTests
{
    [Fact]
    public void Constructor_BuildsConnectionConfig_FromControlPlaneUrlAndApiKey()
    {
        var sut = CreateClient(new CodeActSettings
        {
            OpenSandbox = new OpenSandboxCodeActSettings
            {
                ControlPlaneBaseUrl = "http://sandbox.example.local:9090",
                ApiKey = "secret-token"
            }
        });

        var config = GetPrivateField<ConnectionConfig>(sut, "_connectionConfig");
        Assert.Equal("sandbox.example.local:9090", config.Domain);
        Assert.Equal(ConnectionProtocol.Http, config.Protocol);
        Assert.Equal("secret-token", config.ApiKey);
    }

    [Theory]
    [InlineData("python", "python")]
    [InlineData("Py", "python")]
    [InlineData("javascript", "javascript")]
    [InlineData("ts", "typescript")]
    [InlineData("go", "go")]
    [InlineData("JAVA", "java")]
    [InlineData("sh", "bash")]
    [InlineData("unknown-lang", "python")]
    public void NormalizeLanguage_MapsAliasesAsExpected(string input, string expected)
    {
        var actual = InvokePrivateStatic<string>("NormalizeLanguage", input);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("stdout", OpenSandboxCodeEventTypes.Stdout)]
    [InlineData("stderr", OpenSandboxCodeEventTypes.Stderr)]
    [InlineData("error", OpenSandboxCodeEventTypes.Error)]
    [InlineData("result", OpenSandboxCodeEventTypes.Completed)]
    [InlineData("done", OpenSandboxCodeEventTypes.Completed)]
    [InlineData("heartbeat", OpenSandboxCodeEventTypes.Unknown)]
    public void NormalizeType_MapsEventTypeAsExpected(string input, string expected)
    {
        var actual = InvokePrivateStatic<string>("NormalizeType", input);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void BuildResource_IncludesCpuAndMemory_WhenProvided()
    {
        var options = new OpenSandboxSessionOptions
        {
            CpuLimit = 2,
            MemoryMb = 1024
        };

        var resource = InvokePrivateStatic<Dictionary<string, string>>("BuildResource", options);

        Assert.Equal("2", resource["cpu"]);
        Assert.Equal("1024", resource["memory_mb"]);
    }

    [Fact]
    public void ToStringDictionary_FiltersNullOrEmptyValues()
    {
        var source = new Dictionary<string, object?>
        {
            ["a"] = 1,
            ["b"] = null,
            [""] = "ignored",
            ["c"] = true
        };

        var result = InvokePrivateStatic<Dictionary<string, string>>("ToStringDictionary", source);

        Assert.Equal("1", result["a"]);
        Assert.Equal("True", result["c"]);
        Assert.False(result.ContainsKey("b"));
        Assert.False(result.ContainsKey(string.Empty));
    }

    private static OpenSandboxHttpCodeClient CreateClient(CodeActSettings settings)
    {
        return new OpenSandboxHttpCodeClient(settings);
    }

    private static T InvokePrivateStatic<T>(string methodName, params object[] args)
    {
        var method = typeof(OpenSandboxHttpCodeClient).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        var result = method!.Invoke(null, args);
        Assert.NotNull(result);
        return (T)result!;
    }

    private static T GetPrivateField<T>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        var value = field!.GetValue(instance);
        Assert.NotNull(value);
        return (T)value!;
    }
}
