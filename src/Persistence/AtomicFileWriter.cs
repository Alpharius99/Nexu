using System.Text;

namespace Nexu.Persistence;

public static class AtomicFileWriter
{
    public static void Write(string filePath, string content)
    {
        var tempPath = filePath + ".tmp";
        File.WriteAllText(tempPath, content, Encoding.UTF8);
        File.Move(tempPath, filePath, overwrite: true);
    }
}
