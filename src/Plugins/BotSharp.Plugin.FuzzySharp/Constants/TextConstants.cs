
namespace BotSharp.Plugin.FuzzySharp.Constants
{
    public static class TextConstants
    {
        /// <summary>
        /// Characters that need to be separated during tokenization (by adding spaces before and after)
        /// Includes: parentheses, brackets, braces, punctuation marks, special symbols, etc.
        /// This ensures "(IH)" is split into "(", "IH", ")"
        /// </summary>
        public static readonly char[] SeparatorChars =
        {
            // Parentheses and brackets
            '(', ')', '[', ']', '{', '}',
            // Punctuation marks
            ',', '.', ';', ':', '!', '?',
            // Special symbols
            '=', '@', '#', '$', '%', '^', '&', '*', '+', '-', '\\', '|', '<', '>', '~', '`'
        };

        /// <summary>
        /// Whitespace characters used as token separators during tokenization.
        /// Includes: space, tab, newline, and carriage return.
        /// </summary>
        public static readonly char[] TokenSeparators =
        {
            ' ', '\t', '\n', '\r'
        };
    }
}