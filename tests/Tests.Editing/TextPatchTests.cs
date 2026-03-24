using Nexu.Editing;

namespace Nexu.Tests.Editing;

public class TextPatchTests
{
    [Fact]
    public void ApplyTo_ReplacesCorrectSpan()
    {
        var patch = new TextPatch(1, 3, "bcd", "XY");
        Assert.Equal("aXYe", patch.ApplyTo("abcde"));
    }

    [Fact]
    public void ApplyTo_PureInsertion_Length0()
    {
        var patch = new TextPatch(2, 0, "", "XX");
        Assert.Equal("abXXcde", patch.ApplyTo("abcde"));
    }

    [Fact]
    public void ApplyTo_ThrowsWhenOldTextMismatch()
    {
        var patch = new TextPatch(0, 3, "abc", "x");
        Assert.Throws<InvalidOperationException>(() => patch.ApplyTo("ZZZ"));
    }

    [Fact]
    public void ApplyTo_ThrowsWhenOutOfRange()
    {
        var patch = new TextPatch(-1, 1, "", "x");
        Assert.Throws<ArgumentOutOfRangeException>(() => patch.ApplyTo("hello"));
    }

    [Fact]
    public void Invert_ProducesUndoPatch()
    {
        var patch = new TextPatch(1, 2, "ab", "XY");
        var inverted = patch.Invert();
        Assert.Equal(new TextPatch(1, 2, "XY", "ab"), inverted);
    }

    [Fact]
    public void RecordEquality_SameValues_AreEqual()
    {
        var a = new TextPatch(0, 3, "abc", "xyz");
        var b = new TextPatch(0, 3, "abc", "xyz");
        Assert.Equal(a, b);
    }
}
