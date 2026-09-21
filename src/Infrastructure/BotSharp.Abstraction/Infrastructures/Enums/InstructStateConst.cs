namespace BotSharp.Abstraction.Infrastructures.Enums;

/// <summary>
/// Conversation states carrying the input of an instruct mode request.
/// </summary>
public static class InstructStateConst
{
    public const string INSTRUCTION = "instruction";
    public const string INPUT_TEXT = "input_text";
    public const string TEMPLATE_NAME = "template_name";
    public const string CODE_OPTIONS = "code_options";
    public const string FILE_OPTIONS = "file_options";
    public const string FILE_COUNT = "file_count";
    public const string FILE_URLS = "file_urls";
}
