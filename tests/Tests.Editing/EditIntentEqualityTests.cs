using Nexu.Domain;
using Nexu.Editing;

namespace Nexu.Tests.Editing;

public class EditIntentEqualityTests
{
    private static readonly NodeId Id1 = new(Guid.Parse("00000000-0000-0000-0000-000000000001"));
    private static readonly NodeId Id2 = new(Guid.Parse("00000000-0000-0000-0000-000000000002"));

    [Fact]
    public void RenameProperty_RecordEquality()
    {
        var a = new RenameProperty(Id1, 0, 5, "old", "new");
        var b = new RenameProperty(Id1, 0, 5, "old", "new");
        Assert.Equal(a, b);
        Assert.NotEqual(a, new RenameProperty(Id2, 0, 5, "old", "new"));
    }

    [Fact]
    public void SetScalarValue_RecordEquality()
    {
        var a = new SetScalarValue(Id1, 10, 15, "old", "new");
        var b = new SetScalarValue(Id1, 10, 15, "old", "new");
        Assert.Equal(a, b);
        Assert.NotEqual(a, new SetScalarValue(Id1, 10, 15, "old", "different"));
    }

    [Fact]
    public void AddProperty_RecordEquality()
    {
        var a = new AddProperty(Id1, 0, 10, 8, "  ", "key", "1");
        var b = new AddProperty(Id1, 0, 10, 8, "  ", "key", "1");
        Assert.Equal(a, b);
        Assert.NotEqual(a, new AddProperty(Id1, 0, 10, 8, "  ", "key", "2"));
    }

    [Fact]
    public void AddArrayItem_RecordEquality()
    {
        var a = new AddArrayItem(Id1, 0, 5, 3, "  ", "42");
        var b = new AddArrayItem(Id1, 0, 5, 3, "  ", "42");
        Assert.Equal(a, b);
        Assert.NotEqual(a, new AddArrayItem(Id1, 0, 5, 3, "  ", "99"));
    }

    [Fact]
    public void RemoveNode_RecordEquality()
    {
        var a = new RemoveNode(Id1, 5, 10, 3, 12, 3);
        var b = new RemoveNode(Id1, 5, 10, 3, 12, 3);
        Assert.Equal(a, b);
        Assert.NotEqual(a, new RemoveNode(Id1, 5, 10, 3, 12, 2));
    }
}
