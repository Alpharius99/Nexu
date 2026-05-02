using Avalonia;

namespace Nexu.App;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        var log = Path.Combine(Path.GetTempPath(), "nexu-crash.log");
        File.WriteAllText(log, $"[{DateTime.Now:O}] Main entered\n");

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            File.AppendAllText(log, $"[{DateTime.Now:O}] UnhandledException: {e.ExceptionObject}\n");
        };

        try
        {
            File.AppendAllText(log, $"[{DateTime.Now:O}] Building Avalonia app\n");
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            File.AppendAllText(log, $"[{DateTime.Now:O}] Exited normally\n");
        }
        catch (Exception ex)
        {
            File.AppendAllText(log, $"[{DateTime.Now:O}] CRASH: {ex}\n");
            Console.Error.WriteLine($"CRASH: {ex}");
            throw;
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
