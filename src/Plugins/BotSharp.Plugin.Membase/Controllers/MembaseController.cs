using BotSharp.Abstraction.Graph;
using Microsoft.AspNetCore.Http;

namespace BotSharp.Plugin.Membase.Controllers;

[Authorize]
[ApiController]
public class MembaseController : ControllerBase
{
    private readonly IServiceProvider _services;

    public MembaseController(
        IServiceProvider services)
    {
        _services = services;
    }

    /// <summary>
    /// Execute a Cypher graph query
    /// </summary>
    /// <param name="graphId">The graph identifier</param>
    /// <param name="request">The Cypher query request containing the query and parameters</param>
    /// <returns>Query result with columns and data</returns>
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
            var graph = _services.GetServices<IGraphDb>().First(x => x.Provider == "membase");
            var result = await graph.SearchAsync(query: request.Query, options: new()
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
}
