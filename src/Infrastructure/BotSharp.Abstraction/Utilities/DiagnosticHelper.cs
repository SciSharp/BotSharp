using System.Diagnostics;

namespace BotSharp.Abstraction.Utilities;

public static class DiagnosticHelper
{
    /// <summary>
    /// Gets the current stack trace for debugging purposes.
    /// </summary>
    /// <param name="skipFrames">Number of frames to skip (default 1 to skip this method itself)</param>
    /// <returns>Stack trace string</returns>
    public static string GetCurrentStackTrace(int skipFrames = 1)
    {
        return new StackTrace(skipFrames, true).ToString();
    }
}
