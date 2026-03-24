namespace Nexu.Editing;

public static class IndentationDetector
{
    /// <summary>
    /// Detects the indentation used for children inside a container.
    /// containerStart = position of '{' or '['.
    /// firstChildStart = position of the first child node, or -1 if none.
    /// Returns the indentation string (e.g. "  " or "    ") or "  " as default.
    /// </summary>
    public static string Detect(string rawText, int containerStart, int firstChildStart)
    {
        if (firstChildStart <= containerStart)
            return "  ";

        // Walk backward from firstChildStart to find the start of its line
        int i = firstChildStart - 1;
        while (i >= 0 && rawText[i] != '\n')
            i--;

        // i is now at '\n' or -1 (start of text)
        int lineStart = i + 1;
        int indent = firstChildStart - lineStart;
        return indent > 0 ? new string(' ', indent) : "  ";
    }
}
