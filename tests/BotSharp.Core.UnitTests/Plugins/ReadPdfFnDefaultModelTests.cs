using System.Reflection;
using BotSharp.Plugin.FileHandler.Functions;
using BotSharp.Plugin.FileHandler.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BotSharp.Core.UnitTests.Plugins;

public class ReadPdfFnDefaultModelTests
{
    // Google shut down gemini-2.0-flash on 2026-06-01, so the model that ReadPdfFn
    // falls back to when no FileHandler:Pdf:Reading provider and model are set
    // must be one the Gemini API still serves.
    [Fact]
    public void Falls_back_to_a_gemini_model_that_is_still_served()
    {
        var fn = new ReadPdfFn(null!, NullLogger<ReadPdfFn>.Instance, new FileHandlerSettings());

        var (provider, model) = ((string, string))typeof(ReadPdfFn)
            .GetMethod("GetLlmProviderModel", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(fn, null)!;

        Assert.Equal("google-ai", provider);
        Assert.Equal("gemini-3.8-flash", model);
    }
}
