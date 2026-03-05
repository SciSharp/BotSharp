using BotSharp.Abstraction.Rules;
using System.Text.Json;

namespace BotSharp.Plugin.Membase.Services;

public class DemoRuleConfig : IRuleConfig
{
    private readonly IServiceProvider _services;

    public DemoRuleConfig(
        IServiceProvider services)
    {
        _services = services;
    }

    public string Provider => "membase";

    public async Task<JsonDocument> GetConfigAsync()
    {
        var settings = _services.GetRequiredService<MembaseSettings>();
        var apiKey = settings.ApiKey;
        var projectId = "68503047c5796a8049634a51";
        var graphId = "69a76a0ea77b9871345de795";
        var query = "MATCH%20(a)-[r]-%3E(b)%20WITH%20a,%20r,%20b%20WHERE%20a.agent%20=%20$agent%20AND%20a.trigger%20=%20$trigger%20AND%20b.agent%20=%20$agent%20AND%20b.trigger%20=%20$trigger%20RETURN%20a,%20r,%20b%20LIMIT%20100";

        return JsonDocument.Parse(JsonSerializer.Serialize(new
        {
            source = "membase",
            htmlTag = "iframe",
            url = $"https://console.membase.dev/query-editor/{projectId}?graphId={graphId}&query={query}&token={apiKey}"
        }));
    }
}
