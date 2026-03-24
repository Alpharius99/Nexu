namespace Nexu.Editing;

public sealed record TextPatch(int Start, int Length, string OldText, string NewText)
{
    public string ApplyTo(string text)
    {
        if (Start < 0 || Length < 0 || Start + Length > text.Length)
            throw new ArgumentOutOfRangeException(nameof(Start),
                $"Patch range [{Start}, {Start + Length}) is out of range for text of length {text.Length}.");

        var actual = text.AsSpan(Start, Length);
        if (!actual.SequenceEqual(OldText.AsSpan()))
            throw new InvalidOperationException(
                $"OldText mismatch at position {Start}: expected \"{OldText}\" but found \"{actual}\".");

        return string.Concat(
            text.AsSpan(0, Start),
            NewText.AsSpan(),
            text.AsSpan(Start + Length));
    }

    public TextPatch Invert() => new(Start, NewText.Length, NewText, OldText);
}
