using BotSharp.Plugin.Membase.Models;
using Refit;
using System.Threading.Tasks;

namespace BotSharp.Plugin.Membase.Services;

public interface IMembaseApi
{
    [Post("/graph/{graphId}/node")]
    Task CreateNode(string graphId, [Body] NodeCreationModel node);

    [Get("/graph/{graphId}/node/{nodeId}")]
    Task<Node> GetNode(string graphId, string nodeId);

    [Put("/graph/{graphId}/node/{nodeId}")]
    Task<Node> UpdateNode(string graphId, string nodeId, [Body] NodeUpdateModel node);

    [Delete("/graph/{graphId}/node/{nodeId}")]
    Task<IActionResult> DeleteNode(string graphId, string nodeId);
}
