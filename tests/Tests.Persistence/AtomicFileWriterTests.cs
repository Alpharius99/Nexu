using Nexu.Persistence;

namespace Nexu.Tests.Persistence;

public class AtomicFileWriterTests
{
    private static string TempPath() =>
        Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    [Fact]
    public void Write_CreatesFileWithCorrectContent()
    {
        var path = TempPath();
        try
        {
            AtomicFileWriter.Write(path, "hello world");
            Assert.Equal("hello world", File.ReadAllText(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Write_TempFileIsCleanedUp()
    {
        var path = TempPath();
        try
        {
            AtomicFileWriter.Write(path, "content");
            Assert.False(File.Exists(path + ".tmp"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Write_OverwritesExistingFile()
    {
        var path = TempPath();
        try
        {
            File.WriteAllText(path, "original");
            AtomicFileWriter.Write(path, "updated");
            Assert.Equal("updated", File.ReadAllText(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Write_UsesUtf8Encoding()
    {
        var path = TempPath();
        try
        {
            const string content = "こんにちは";
            AtomicFileWriter.Write(path, content);
            Assert.Equal(content, File.ReadAllText(path, System.Text.Encoding.UTF8));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
