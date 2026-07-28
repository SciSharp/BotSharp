using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.MLTasks.Settings;
using BotSharp.Core.Infrastructures;
using BotSharp.Plugin.MiniMaxAI;
using BotSharp.Plugin.MiniMaxAI.Providers.Chat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using AnthropicProviderHelper = BotSharp.Plugin.AnthropicAI.Providers.ProviderHelper;

namespace BotSharp.LLM.Tests;

public class MiniMaxProviderTests
{
    private static readonly Dictionary<string, string> ExpectedEndpoints = new()
    {
        ["minimax"] = "https://api.minimax.io/v1",
        ["minimax-cn"] = "https://api.minimaxi.com/v1",
        ["minimax-anthropic"] = "https://api.minimax.io/anthropic",
        ["minimax-anthropic-cn"] = "https://api.minimaxi.com/anthropic"
    };

    [Fact]
    public void RegistersEveryProtocolAndRegionAlias()
    {
        var services = new ServiceCollection();
        new MiniMaxAiPlugin().RegisterDI(services, new ConfigurationBuilder().Build());

        var providerTypes = services
            .Where(x => x.ServiceType == typeof(IChatCompletion))
            .Select(x => x.ImplementationType)
            .ToArray();

        providerTypes.ShouldBe([
            typeof(ChatCompletionProvider),
            typeof(OpenAiCnChatCompletionProvider),
            typeof(AnthropicChatCompletionProvider),
            typeof(AnthropicCnChatCompletionProvider)
        ]);
    }

    [Fact]
    public void WebStarterSettingsCoverModelsEndpointsAndCapabilities()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("WebStarter.appsettings.json")
            .Build();
        var providers = configuration.GetSection("LlmProviders").Get<List<LlmProviderSetting>>();

        providers.ShouldNotBeNull();
        foreach (var expected in ExpectedEndpoints)
        {
            var provider = providers.Single(x => x.Provider == expected.Key);
            provider.Models.Select(x => x.Id).ShouldBe(["MiniMax-M3", "MiniMax-M2.7"]);
            provider.Models.ShouldAllBe(x => x.Endpoint == expected.Value);

            var m3 = provider.Models.Single(x => x.Id == "MiniMax-M3");
            m3.ContextWindow.ShouldBe(1_000_000);
            m3.MultiModal.ShouldBeTrue();
            m3.InputModalities.ShouldBe(["text", "image", "video"]);
            m3.Reasoning!.Parameters!["ThinkingType"].Default.ShouldBe(
                expected.Key.Contains("anthropic", StringComparison.Ordinal)
                    ? "disabled"
                    : "adaptive");
            m3.Cost.TextTokenCostTiers!.Count.ShouldBe(4);

            var m27 = provider.Models.Single(x => x.Id == "MiniMax-M2.7");
            m27.ContextWindow.ShouldBe(204_800);
            m27.MultiModal.ShouldBeFalse();
            m27.InputModalities.ShouldBe(["text"]);
            m27.Reasoning!.Parameters!["ThinkingType"].Default.ShouldBe("always_on");
            m27.Cost.CachedTextInputWriteCost.ShouldBe(0.000375f);
        }
    }

    [Fact]
    public void AnthropicClientUsesConfiguredBaseUrl()
    {
        const string providerName = "minimax-anthropic";
        const string modelName = "MiniMax-M3";
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new List<LlmProviderSetting>
        {
            new()
            {
                Provider = providerName,
                Models =
                [
                    new()
                    {
                        Id = modelName,
                        Name = modelName,
                        ApiKey = "test-key",
                        Endpoint = "https://api.minimax.io/anthropic"
                    }
                ]
            }
        });
        services.AddSingleton<ILlmProviderService, LlmProviderService>();

        using var serviceProvider = services.BuildServiceProvider();
        using var client = AnthropicProviderHelper.GetAnthropicClient(providerName, modelName, serviceProvider);

        client.ApiUrlFormat.ShouldBe("https://api.minimax.io/anthropic/{0}/{1}");
    }

    [Fact]
    public void SelectsTextCostTierByServiceTierAndInputLength()
    {
        var cost = new LlmCostSetting
        {
            DefaultServiceTier = "standard",
            TextTokenCostTiers =
            [
                new() { ServiceTier = "standard", InputTokensLessThanOrEqual = 512_000, TextInputCost = 0.0003f },
                new() { ServiceTier = "standard", InputTokensGreaterThan = 512_000, TextInputCost = 0.0006f },
                new() { ServiceTier = "priority", InputTokensLessThanOrEqual = 512_000, TextInputCost = 0.00045f },
                new() { ServiceTier = "priority", InputTokensGreaterThan = 512_000, TextInputCost = 0.0009f }
            ]
        };

        cost.GetTextTokenCostTier(512_000)!.TextInputCost.ShouldBe(0.0003f);
        cost.GetTextTokenCostTier(512_001)!.TextInputCost.ShouldBe(0.0006f);
        cost.GetTextTokenCostTier(512_000, "priority")!.TextInputCost.ShouldBe(0.00045f);
        cost.GetTextTokenCostTier(512_001, "priority")!.TextInputCost.ShouldBe(0.0009f);
    }
}
