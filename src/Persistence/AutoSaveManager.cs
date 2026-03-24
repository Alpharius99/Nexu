namespace Nexu.Persistence;

public sealed class AutoSaveManager : IDisposable
{
    private readonly TimeSpan _delay;
    private readonly Func<Task> _saveCallback;
    private CancellationTokenSource? _cts;

    public AutoSaveManager(TimeSpan delay, Func<Task> saveCallback)
    {
        _delay = delay;
        _saveCallback = saveCallback;
    }

    public void Schedule()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        var cts = new CancellationTokenSource();
        _cts = cts;
        var token = cts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(_delay, token);
                await _saveCallback();
            }
            catch (OperationCanceledException) { }
        });
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
