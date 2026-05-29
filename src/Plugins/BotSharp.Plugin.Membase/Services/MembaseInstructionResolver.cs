using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Templating;
using BotSharp.Abstraction.Templating.Constants;
using BotSharp.Abstraction.Utilities;

namespace BotSharp.Plugin.Membase.Services;

public class MembaseInstructionResolver : IInstructionResolver
{
    private readonly IServiceProvider _services;
    private readonly ILogger<MembaseInstructionResolver> _logger;
    private readonly IConversationStateService _states;

    private const int LIMIT = 300;

    public string Provider => "membase";

    public MembaseInstructionResolver(
        IServiceProvider services,
        ILogger<MembaseInstructionResolver> logger,
        IConversationStateService states)
    {
        _services = services;
        _logger = logger;
        _states = states;
    }

    public async Task<string> ResolveAsync(Agent agent, string instruction, IEnumerable<object?> args, IDictionary<string, object?> kwArgs)
    {
        string? graphId = kwArgs?.TryGetValue("graph_id", out string? gId) == true ? gId : null;
        string? format = kwArgs?.TryGetValue("format", out string? f) == true ? f : null;
        string? startNodeId = kwArgs?.TryGetValue("start_node_id", out string? s) == true ? s : null;
        int hop = kwArgs?.TryGetValue("hop", out var h) == true && h != null ? Convert.ToInt32(h) : 0;

        if (string.IsNullOrEmpty(startNodeId))
        {
            return instruction;
        }

        _states.SetState("start_node_id", startNodeId);
        var graphDb = _services.GetServices<IGraphDb>().First(x => x.Provider.IsEqualTo(Provider));

        if (format.IsEqualTo("complete graph"))
        {
            var query = """
                    MATCH path = (p1)-[r*0..]->(p2)
                    WHERE p1.id = $startNodeId
                    UNWIND relationships(path) AS rel
                    RETURN DISTINCT startNode(rel) AS sourceNode, rel as edge, endNode(rel) AS targetNode
                    LIMIT $limit
                    """;

            var result = await graphDb.ExecuteQueryAsync(query, options: new()
            {
                GraphId = graphId,
                Arguments = new()
                {
                    ["startNodeId"] = startNodeId,
                    ["limit"] = LIMIT
                }
            });

            var data = _states.GetStates();
            var graph = GraphBuilder.Build(result, data);
            var renderData = data.ToDictionary(x => x.Key, x => (object)x.Value);
            renderData["workflow_graph"] = JsonSerializer.Serialize(graph.GetGraphInfo(), BotSharpOptions.defaultJsonOptions);
            renderData[TemplateRenderConstant.RENDER_AGENT] = agent;

            var render = _services.GetRequiredService<ITemplateRender>();
            instruction = render.Render(instruction, renderData);
        }
        else if (format.IsEqualTo("partial graph"))
        {
            var nextNodeId = _states.GetState("next_node_id");
            if (string.IsNullOrEmpty(nextNodeId))
            {
                nextNodeId = startNodeId;
                _states.SetState("next_node_id", nextNodeId);
            }

            hop = hop > 0 ? hop : 1;
            var query = $"""
                        MATCH path = (p1)-[r*0..{hop}]->(p2)
                        WHERE p1.id = $startNodeId
                        UNWIND relationships(path) AS rel
                        RETURN DISTINCT startNode(rel) AS sourceNode, rel as edge, endNode(rel) AS targetNode
                        LIMIT $limit
                        """;

            var result = await graphDb.ExecuteQueryAsync(query, options: new()
            {
                GraphId = graphId,
                Arguments = new()
                {
                    ["startNodeId"] = nextNodeId,
                    ["limit"] = LIMIT
                }
            });

            if (!result.Values.IsNullOrEmpty())
            {
                var data = _states.GetStates();
                var graph = GraphBuilder.Build(result, data);
                var renderData = data.ToDictionary(x => x.Key, x => (object)x.Value);
                renderData["workflow_graph"] = JsonSerializer.Serialize(graph.GetGraphInfo(), BotSharpOptions.defaultJsonOptions);
                renderData[TemplateRenderConstant.RENDER_AGENT] = agent;

                var render = _services.GetRequiredService<ITemplateRender>();
                instruction = render.Render(instruction, renderData);
            }
        }

        return instruction;
    }
}
