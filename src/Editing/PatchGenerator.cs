using System.Text;

namespace Nexu.Editing;

public static class PatchGenerator
{
    public static TextPatch Generate(EditIntent intent, string rawText) => intent switch
    {
        RenameProperty r => GenerateRename(r, rawText),
        SetScalarValue s => GenerateSetScalar(s, rawText),
        AddProperty a => GenerateAddProperty(a, rawText),
        AddArrayItem a => GenerateAddArrayItem(a, rawText),
        RemoveNode r => GenerateRemoveNode(r, rawText),
        _ => throw new ArgumentOutOfRangeException(nameof(intent), $"Unknown intent type: {intent.GetType().Name}")
    };

    // ── RenameProperty ────────────────────────────────────────────────────────

    private static TextPatch GenerateRename(RenameProperty intent, string rawText)
    {
        var oldRaw = "\"" + JsonEscapeString(intent.OldName) + "\"";
        var newRaw = "\"" + JsonEscapeString(intent.NewName) + "\"";
        return new TextPatch(intent.KeyStart, intent.KeyEnd - intent.KeyStart, oldRaw, newRaw);
    }

    // ── SetScalarValue ────────────────────────────────────────────────────────

    private static TextPatch GenerateSetScalar(SetScalarValue intent, string rawText)
    {
        _ = rawText; // positions validated by ApplyTo
        return new TextPatch(intent.ValueStart, intent.ValueEnd - intent.ValueStart, intent.OldRawText, intent.NewRawText);
    }

    // ── AddProperty ──────────────────────────────────────────────────────────

    private static TextPatch GenerateAddProperty(AddProperty intent, string rawText)
    {
        var nl = DetectNewline(rawText);
        var escapedKey = JsonEscapeString(intent.NewKey);

        // Compute the object's own indentation by looking at what precedes '{'
        var objIndent = DetectLineIndent(rawText, intent.ParentObjectStart);

        if (intent.LastPropertyEnd == -1)
        {
            // Empty object: replace everything between '{' and '}' with new content
            var innerStart = intent.ParentObjectStart + 1;
            var innerEnd = intent.ParentObjectEnd - 1;
            var oldInner = rawText.Substring(innerStart, innerEnd - innerStart);
            var newInner = $"{nl}{intent.Indentation}\"{escapedKey}\": {intent.NewValueRaw}{nl}{objIndent}";
            return new TextPatch(innerStart, innerEnd - innerStart, oldInner, newInner);
        }
        // Non-empty: replace the gap between last property end and the '}'
        var gapStart = intent.LastPropertyEnd;
        var gapEnd = intent.ParentObjectEnd - 1; // points to '}'
        var oldGap = rawText.Substring(gapStart, gapEnd - gapStart);
        var newGap = $",{nl}{intent.Indentation}\"{escapedKey}\": {intent.NewValueRaw}{nl}{objIndent}";
        return new TextPatch(gapStart, gapEnd - gapStart, oldGap, newGap);
    }

    // ── AddArrayItem ──────────────────────────────────────────────────────────

    private static TextPatch GenerateAddArrayItem(AddArrayItem intent, string rawText)
    {
        // Detect if the array spans multiple lines
        var arraySpan = rawText.Substring(intent.ParentArrayStart,
            intent.ParentArrayEnd - intent.ParentArrayStart);
        var isMultiline = arraySpan.Contains('\n');

        var nl = DetectNewline(rawText);
        var arrIndent = DetectLineIndent(rawText, intent.ParentArrayStart);

        if (intent.LastElementEnd == -1)
        {
            // Empty array: replace ']'
            string insertion;
            if (isMultiline)
                insertion = $"{nl}{intent.Indentation}{intent.NewValueRaw}{nl}{arrIndent}]";
            else
                insertion = $"{intent.NewValueRaw}]";

            return new TextPatch(
                intent.ParentArrayEnd - 1,
                1,
                "]",
                insertion);
        }
        // Non-empty: replace gap between last element end and ']'
        var gapStart = intent.LastElementEnd;
        var gapEnd = intent.ParentArrayEnd - 1;
        var oldGap = rawText.Substring(gapStart, gapEnd - gapStart);

        string newGap;
        if (isMultiline)
            newGap = $",{nl}{intent.Indentation}{intent.NewValueRaw}{nl}{arrIndent}";
        else
            newGap = $", {intent.NewValueRaw}";

        return new TextPatch(gapStart, gapEnd - gapStart, oldGap, newGap);
    }

    // ── RemoveNode ────────────────────────────────────────────────────────────

    private static TextPatch GenerateRemoveNode(RemoveNode intent, string rawText)
    {
        if (intent.SiblingCount == 1)
        {
            // Only child: blank out everything between delimiters
            // Find the delimiter before and after
            // The node is the only child; remove from after '{' or '[' to before '}' or ']'
            // We look at chars just before NodeStart and just after NodeEnd
            int innerStart = FindInnerStart(rawText, intent.NodeStart);
            int innerEnd = FindInnerEnd(rawText, intent.NodeEnd);
            var oldText = rawText.Substring(innerStart, innerEnd - innerStart);
            return new TextPatch(innerStart, innerEnd - innerStart, oldText, "");
        }
        if (intent.PrevSiblingEnd == -1)
        {
            // First child: remove node + comma after + whitespace up to next sibling
            var commaPos = FindCommaAfter(rawText, intent.NodeEnd);
            if (commaPos == -1)
                commaPos = intent.NodeEnd;

            var removeEnd = ScanForwardPastWhitespace(rawText, commaPos + 1);
            // Don't overshoot into next sibling
            if (intent.NextSiblingStart >= 0 && removeEnd > intent.NextSiblingStart)
                removeEnd = intent.NextSiblingStart;

            // Also remove leading whitespace before NodeStart (back to newline)
            var removeStart = ScanBackToNewlineOrDelimiter(rawText, intent.NodeStart);
            var oldText = rawText.Substring(removeStart, removeEnd - removeStart);
            return new TextPatch(removeStart, removeEnd - removeStart, oldText, "");
        }
        if (intent.NextSiblingStart == -1)
        {
            // Last child: find comma before node and remove from comma through NodeEnd + trailing whitespace
            var commaPos = FindCommaBefore(rawText, intent.NodeStart);
            if (commaPos == -1)
                commaPos = intent.NodeStart;

            // Include any trailing whitespace after node end (up to but not including closing delimiter)
            var removeEnd = ScanForwardPastWhitespace(rawText, intent.NodeEnd);
            var oldText = rawText.Substring(commaPos, removeEnd - commaPos);
            return new TextPatch(commaPos, removeEnd - commaPos, oldText, "");
        }
        else
        {
            // Middle child: same as first child (remove node + following comma + whitespace)
            var commaPos = FindCommaAfter(rawText, intent.NodeEnd);
            if (commaPos == -1)
                commaPos = intent.NodeEnd;

            var removeEnd = ScanForwardPastWhitespace(rawText, commaPos + 1);
            if (intent.NextSiblingStart >= 0 && removeEnd > intent.NextSiblingStart)
                removeEnd = intent.NextSiblingStart;

            var removeStart = ScanBackToNewlineOrDelimiter(rawText, intent.NodeStart);
            var oldText = rawText.Substring(removeStart, removeEnd - removeStart);
            return new TextPatch(removeStart, removeEnd - removeStart, oldText, "");
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static int FindInnerStart(string text, int nodeStart)
    {
        // Walk back past whitespace to find content start after delimiter
        int i = nodeStart - 1;
        while (i >= 0 && text[i] is ' ' or '\t' or '\r' or '\n')
            i--;
        // i is now at the delimiter '{' or '[' or at a position before node
        return i + 1;
    }

    private static int FindInnerEnd(string text, int nodeEnd)
    {
        int i = nodeEnd;
        while (i < text.Length && text[i] is ' ' or '\t' or '\r' or '\n')
            i++;
        return i;
    }

    private static int FindCommaAfter(string text, int pos)
    {
        int i = pos;
        while (i < text.Length && text[i] is ' ' or '\t' or '\r' or '\n')
            i++;
        if (i < text.Length && text[i] == ',')
            return i;
        return -1;
    }

    private static int FindCommaBefore(string text, int pos)
    {
        int i = pos - 1;
        while (i >= 0 && text[i] is ' ' or '\t' or '\r' or '\n')
            i--;
        if (i >= 0 && text[i] == ',')
            return i;
        return -1;
    }

    private static int ScanForwardPastWhitespace(string text, int pos)
    {
        while (pos < text.Length && text[pos] is ' ' or '\t' or '\r' or '\n')
            pos++;
        return pos;
    }

    private static int ScanBackToNewlineOrDelimiter(string text, int pos)
    {
        int i = pos - 1;
        while (i >= 0 && text[i] is ' ' or '\t')
            i--;
        // If we hit a newline, include it in the removal to avoid blank lines
        if (i >= 0 && text[i] == '\n')
            return i + 1; // keep the newline, remove the indent
        // Otherwise keep back to after the delimiter
        return i + 1;
    }

    private static string DetectLineIndent(string text, int pos)
    {
        int i = pos - 1;
        while (i >= 0 && text[i] is ' ' or '\t')
            i--;
        if (i < 0 || text[i] == '\n')
        {
            int lineStart = i + 1;
            return text.Substring(lineStart, pos - lineStart);
        }
        return "";
    }

    public static string JsonEscapeString(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\b': sb.Append("\\b"); break;
                case '\f': sb.Append("\\f"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < 0x20)
                        sb.Append($"\\u{(int)c:x4}");
                    else
                        sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }

    private static string DetectNewline(string text)
    {
        foreach (var c in text)
        {
            if (c == '\r')
                return text.Contains("\r\n") ? "\r\n" : "\r";
            if (c == '\n')
                return "\n";
        }
        return "\n";
    }
}
