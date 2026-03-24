using Nexu.Persistence;

namespace Nexu.Tests.Persistence;

public class AutoSaveManagerTests
{
    [Fact]
    public async Task Schedule_FiresCallbackAfterDelay()
    {
        var fired = false;
        using var manager = new AutoSaveManager(TimeSpan.FromMilliseconds(50), () =>
        {
            fired = true;
            return Task.CompletedTask;
        });

        manager.Schedule();
        await Task.Delay(200);

        Assert.True(fired);
    }

    [Fact]
    public async Task Schedule_Reschedule_OnlyOneCallbackFires()
    {
        var count = 0;
        using var manager = new AutoSaveManager(TimeSpan.FromMilliseconds(100), () =>
        {
            count++;
            return Task.CompletedTask;
        });

        manager.Schedule();
        manager.Schedule();
        manager.Schedule();
        await Task.Delay(400);

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Dispose_CancelsPendingSave()
    {
        var fired = false;
        var manager = new AutoSaveManager(TimeSpan.FromMilliseconds(100), () =>
        {
            fired = true;
            return Task.CompletedTask;
        });

        manager.Schedule();
        manager.Dispose();
        await Task.Delay(300);

        Assert.False(fired);
    }
}
