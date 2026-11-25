using System.Text.Json;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.MLTasks.Filters;
using BotSharp.Abstraction.MLTasks.Settings;
using BotSharp.OpenAPI.Controllers;
using Moq;
using Xunit;

namespace BotSharp.Core.UnitTests.Infrastructures;

/// <summary>
/// The model lists any signed-in user can read must not carry a model's api key or endpoint, and leaving them out must
/// not touch the settings the server itself keeps calling the models with.
/// </summary>
public class LlmProviderControllerTests
{
    private const string Key = "sk-test-secret-key";
    private const string Endpoint = "https://private-resource.example.test/";

    private static LlmModelSetting Model(string name, LlmModelType type = LlmModelType.Chat) => new()
    {
        Id = name,
        Name = name,
        ApiKey = Key,
        Endpoint = Endpoint,
        ApiVersion = "2024-10-21",
        Type = type,
        Capabilities = [LlmModelCapability.Chat],
        Reasoning = new ReasoningSetting { Temperature = 1 }
    };

    private static (LlmProviderController Controller, List<LlmProviderSetting> Kept) Create()
    {
        // The same instances every call, as a service that caches its settings would hand out
        var kept = new List<LlmProviderSetting>
        {
            new() { Provider = "openai", Models = [Model("model-a"), Model("embedding-a", LlmModelType.Embedding)] }
        };
        var llm = new Mock<ILlmProviderService>();
        llm.Setup(x => x.GetProviderModels("openai")).Returns(kept[0].Models);
        llm.Setup(x => x.GetLlmConfigs(It.IsAny<LlmConfigFilter?>())).Returns(kept);
        return (new LlmProviderController(Mock.Of<IServiceProvider>(), llm.Object), kept);
    }

    [Fact]
    public void Provider_models_leave_out_the_key_and_endpoint_and_keep_the_rest()
    {
        var (controller, kept) = Create();

        var model = Assert.Single(controller.GetLlmProviderModels("openai"));

        Assert.Equal(("model-a", string.Empty, (string?)null), (model.Name, model.ApiKey, model.Endpoint));
        Assert.Equal("2024-10-21", model.ApiVersion);
        Assert.Equal(new[] { LlmModelCapability.Chat }, model.Capabilities);
        Assert.Equal(1, model.Reasoning?.Temperature);
        Assert.DoesNotContain(Key, JsonSerializer.Serialize(model));
        Assert.All(kept[0].Models, x => Assert.Equal((Key, Endpoint), (x.ApiKey, x.Endpoint)));
    }

    [Fact]
    public void Llm_configs_leave_out_the_key_and_endpoint_of_every_model()
    {
        var (controller, kept) = Create();

        var configs = controller.GetLlmConfigs(new LlmConfigFilter());

        var provider = Assert.Single(configs);
        Assert.Equal("openai", provider.Provider);
        Assert.Equal(new[] { "model-a", "embedding-a" }, provider.Models.Select(x => x.Name));
        var json = JsonSerializer.Serialize(configs);
        Assert.DoesNotContain(Key, json);
        Assert.DoesNotContain("private-resource", json);
        Assert.All(kept[0].Models, x => Assert.Equal((Key, Endpoint), (x.ApiKey, x.Endpoint)));
    }
}
