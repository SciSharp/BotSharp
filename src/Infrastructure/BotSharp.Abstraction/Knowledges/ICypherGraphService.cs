namespace BotSharp.Abstraction.Knowledges;

/// <summary>
/// Graph-based semantic knowledge service that supports complex relationships and connections between entities.
/// This service allows for executing Cypher queries to traverse and analyze graph data structures.
/// </summary>
public interface ICypherGraphService
{
    Task<GraphQueryResult> Execute(string graphId, string query, Dictionary<string, object>? args = null);

    Task<GraphNode> MergeNode(string graphId, GraphNode node);

    Task<bool> DeleteNode(string graphId, string nodeId);
}
