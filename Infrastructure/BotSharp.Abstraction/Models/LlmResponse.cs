using Abstraction.Enums;

namespace Abstraction.Models;

public class LlmResponse
{
    public string Model { get; set; } = string.Empty;
    public LlmResponseExtractType Format { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool Success { get; set; }
}
