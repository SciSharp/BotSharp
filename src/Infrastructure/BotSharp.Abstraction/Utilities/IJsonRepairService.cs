namespace BotSharp.Abstraction.Utilities;

/// <summary>
/// Service for repairing malformed JSON using LLM.
/// </summary>
public interface IJsonRepairService
{
    /// <summary>
    /// Repair malformed JSON and deserialize to target type.
    /// </summary>
    /// <typeparam name="T">Target type</typeparam>
    /// <param name="malformedJson">The malformed JSON string</param>
    /// <returns>Deserialized object or default if repair fails</returns>
    Task<T?> RepairAndDeserialize<T>(string malformedJson);

    /// <summary>
    /// Repair malformed JSON string.
    /// </summary>
    /// <param name="malformedJson">The malformed JSON string</param>
    /// <returns>Repaired JSON string</returns>
    Task<string> Repair(string malformedJson);
}

