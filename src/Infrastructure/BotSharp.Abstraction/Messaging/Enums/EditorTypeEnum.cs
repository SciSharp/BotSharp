namespace BotSharp.Abstraction.Messaging.Enums;

public static class EditorTypeEnum
{
    /// <summary>
    /// Disable user input freeform text
    /// </summary>
    public const string None = "none";
    public const string Text = "text";
    public const string Address = "address";
    public const string Phone = "phone";
    public const string DateTimePicker = "datetime-picker";
    public const string DateTimeRangePicker = "datetime-range-picker";
    public const string Email = "email";
    public const string File = "file";

    /// <summary>
    /// Regex, set the expression in editor_attributes
    /// </summary>
    public const string Regex = "regex";
}
