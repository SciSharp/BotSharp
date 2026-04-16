using BotSharp.Abstraction.Graph;
using BotSharp.Plugin.Membase.Interfaces;
using Microsoft.AspNetCore.Http;

namespace BotSharp.Plugin.Membase.Controllers;

[Authorize]
[ApiController]
public class MembaseController : ControllerBase
{
    private const string GraphDbProvider = "membase";
    private readonly IServiceProvider _services;
    private readonly IMembaseApi _membaseApi;

    public MembaseController(
        IServiceProvider services,
        IMembaseApi membaseApi)
    {
        _services = services;
        _membaseApi = membaseApi;
    }

    /// <summary>
    /// Get graph information
    /// </summary>
    /// <param name="graphId">The graph identifier</param>
    /// <returns>Graph information</returns>
#if DEBUG
    [AllowAnonymous]
#endif
    [HttpGet("/membase/{graphId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetGraphInfo(string graphId)
    {
        if (string.IsNullOrWhiteSpace(graphId))
        {
            return BadRequest("Graph ID cannot be empty.");
        }

        try
        {
            var graphInfo = await _membaseApi.GetGraphInfoAsync(graphId);
            return Ok(graphInfo);
        }
        catch (Exception ex)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving graph information.", error = ex.Message });
        }
    }

    /// <summary>
    /// Execute a Cypher graph query
    /// </summary>
    /// <param name="graphId">The graph identifier</param>
    /// <param name="request">The Cypher query request containing the query and parameters</param>
    /// <returns>Query result with columns and data</returns>
#if DEBUG
    [AllowAnonymous]
#endif
    [HttpPost("/membase/{graphId}/query")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExecuteGraphQuery(string graphId, [FromBody] CypherQueryRequest request)
    {
        if (string.IsNullOrWhiteSpace(graphId))
        {
            return BadRequest("Graph ID cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(request?.Query))
        {
            return BadRequest("Query cannot be empty.");
        }

        try
        {
            var graph = _services.GetServices<IGraphDb>().First(x => x.Provider == GraphDbProvider);
            var result = await graph.ExecuteQueryAsync(query: request.Query, options: new()
            {
                GraphId = graphId,
                Arguments = request.Parameters
            });
            return Ok(new
            {
                Columns = result.Keys,
                Items = result.Values
            });
        }
        catch (Exception ex)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while executing the query.", error = ex.Message });
        }
    }

    /// <summary>
    /// Get a node from the graph
    /// </summary>
    /// <param name="graphId">The graph identifier</param>
    /// <param name="nodeId">The node identifier</param>
    /// <returns>The node</returns>
#if DEBUG
    [AllowAnonymous]
#endif
    [HttpGet("/membase/{graphId}/node/{nodeId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetNode(string graphId, string nodeId)
    {
        if (string.IsNullOrWhiteSpace(graphId))
        {
            return BadRequest("Graph ID cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(nodeId))
        {
            return BadRequest("Node ID cannot be empty.");
        }

        try
        {
            var graph = _services.GetServices<IGraphDb>().First(x => x.Provider == GraphDbProvider);
            var node = await graph.GetNodeAsync(graphId, nodeId);
            return Ok(node);
        }
        catch (Exception ex)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving the node.", error = ex.Message });
        }
    }

    /// <summary>
    /// Create a node in the graph
    /// </summary>
    /// <param name="graphId">The graph identifier</param>
    /// <param name="request">The node creation model</param>
    /// <returns>The created node</returns>
#if DEBUG
    [AllowAnonymous]
#endif
    [HttpPost("/membase/{graphId}/node")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateNode(string graphId, [FromBody] NodeCreationModel request)
    {
        if (string.IsNullOrWhiteSpace(graphId))
        {
            return BadRequest("Graph ID cannot be empty.");
        }

        if (request == null)
        {
            return BadRequest("Node creation model cannot be null.");
        }

        try
        {
            var graph = _services.GetServices<IGraphDb>().First(x => x.Provider == GraphDbProvider);
            var node = await graph.CreateNodeAsync(graphId, new GraphNodeCreationRequest
            {
                Id = request.Id,
                Labels = request.Labels,
                Properties = request.Properties,
                Time = request.Time
            });
            return Ok(node);
        }
        catch (Exception ex)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while creating the node.", error = ex.Message });
        }
    }

    /// <summary>
    /// Merge a node in the graph
    /// </summary>
    /// <param name="graphId">The graph identifier</param>
    /// <param name="request">The node update model</param>
    /// <returns>The merged node</returns>
#if DEBUG
    [AllowAnonymous]
#endif
    [HttpPut("/membase/{graphId}/node/merge")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> MergeNode(string graphId, [FromBody] NodeUpdateModel request)
    {
        if (string.IsNullOrWhiteSpace(graphId))
        {
            return BadRequest("Graph ID cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(request?.Id))
        {
            return BadRequest("Node ID cannot be empty.");
        }

        try
        {
            var graph = _services.GetServices<IGraphDb>().First(x => x.Provider == GraphDbProvider);
            var node = await graph.MergeNodeAsync(graphId, request.Id, new GraphNodeUpdateRequest
            {
                Id = request.Id,
                Labels = request.Labels,
                Properties = request.Properties,
                Time = request.Time
            });
            return Ok(node);
        }
        catch (Exception ex)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while merging the node.", error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a node from the graph
    /// </summary>
    /// <param name="graphId">The graph identifier</param>
    /// <param name="nodeId">The node identifier</param>
    /// <returns>Delete response</returns>
#if DEBUG
    [AllowAnonymous]
#endif
    [HttpDelete("/membase/{graphId}/node/{nodeId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteNode(string graphId, string nodeId)
    {
        if (string.IsNullOrWhiteSpace(graphId))
        {
            return BadRequest("Graph ID cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(nodeId))
        {
            return BadRequest("Node ID cannot be empty.");
        }

        try
        {
            var graph = _services.GetServices<IGraphDb>().First(x => x.Provider == GraphDbProvider);
            await graph.DeleteNodeAsync(graphId, nodeId);
            return Ok("done");
        }
        catch (Exception ex)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while deleting the node.", error = ex.Message });
        }
    }

    /// <summary>
    /// Get an edge from the graph
    /// </summary>
    /// <param name="graphId">The graph identifier</param>
    /// <param name="edgeId">The edge identifier</param>
    /// <returns>The edge</returns>
#if DEBUG
    [AllowAnonymous]
#endif
    [HttpGet("/membase/{graphId}/edge/{edgeId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetEdge(string graphId, string edgeId)
    {
        if (string.IsNullOrWhiteSpace(graphId))
        {
            return BadRequest("Graph ID cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(edgeId))
        {
            return BadRequest("Edge ID cannot be empty.");
        }

        try
        {
            var graph = _services.GetServices<IGraphDb>().First(x => x.Provider == GraphDbProvider);
            var edge = await graph.GetEdgeAsync(graphId, edgeId);
            return Ok(edge);
        }
        catch (Exception ex)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving the edge.", error = ex.Message });
        }
    }

    /// <summary>
    /// Create an edge in the graph
    /// </summary>
    /// <param name="graphId">The graph identifier</param>
    /// <param name="request">The edge creation model</param>
    /// <returns>The created edge</returns>
#if DEBUG
    [AllowAnonymous]
#endif
    [HttpPost("/membase/{graphId}/edge")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateEdge(string graphId, [FromBody] EdgeCreationModel request)
    {
        if (string.IsNullOrWhiteSpace(graphId))
        {
            return BadRequest("Graph ID cannot be empty.");
        }

        if (request == null)
        {
            return BadRequest("Edge creation model cannot be null.");
        }

        if (string.IsNullOrWhiteSpace(request.SourceNodeId))
        {
            return BadRequest("Source node ID cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(request.TargetNodeId))
        {
            return BadRequest("Target node ID cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(request.Type))
        {
            return BadRequest("Edge type cannot be empty.");
        }

        try
        {
            var graph = _services.GetServices<IGraphDb>().First(x => x.Provider == GraphDbProvider);
            var edge = await graph.CreateEdgeAsync(graphId, new GraphEdgeCreationRequest
            {
                Id = request.Id,
                SourceNodeId = request.SourceNodeId,
                TargetNodeId = request.TargetNodeId,
                Type = request.Type,
                Directed = request.Directed,
                Weight = request.Weight,
                Properties = request.Properties
            });
            return Ok(edge);
        }
        catch (Exception ex)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while creating the edge.", error = ex.Message });
        }
    }

    /// <summary>
    /// Update an edge in the graph
    /// </summary>
    /// <param name="graphId">The graph identifier</param>
    /// <param name="request">The edge update model</param>
    /// <returns>The updated edge</returns>
#if DEBUG
    [AllowAnonymous]
#endif
    [HttpPut("/membase/{graphId}/edge")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateEdge(string graphId, [FromBody] EdgeUpdateModel request)
    {
        if (string.IsNullOrWhiteSpace(graphId))
        {
            return BadRequest("Graph ID cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(request?.Id))
        {
            return BadRequest("Edge ID cannot be empty.");
        }

        try
        {
            var graph = _services.GetServices<IGraphDb>().First(x => x.Provider == GraphDbProvider);
            var edge = await graph.UpdateEdgeAsync(graphId, request.Id, new GraphEdgeUpdateRequest
            {
                Id = request.Id,
                Properties = request.Properties
            });
            return Ok(edge);
        }
        catch (Exception ex)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while updating the edge.", error = ex.Message });
        }
    }

    /// <summary>
    /// Delete an edge from the graph
    /// </summary>
    /// <param name="graphId">The graph identifier</param>
    /// <param name="edgeId">The edge identifier</param>
    /// <returns>Delete response</returns>
#if DEBUG
    [AllowAnonymous]
#endif
    [HttpDelete("/membase/{graphId}/edge/{edgeId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteEdge(string graphId, string edgeId)
    {
        if (string.IsNullOrWhiteSpace(graphId))
        {
            return BadRequest("Graph ID cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(edgeId))
        {
            return BadRequest("Edge ID cannot be empty.");
        }

        try
        {
            var graph = _services.GetServices<IGraphDb>().First(x => x.Provider == GraphDbProvider);
            await graph.DeleteEdgeAsync(graphId, edgeId);
            return Ok("done");
        }
        catch (Exception ex)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while deleting the edge.", error = ex.Message });
        }
    }
}
