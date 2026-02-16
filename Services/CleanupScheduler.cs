using System;
using System.Threading;
using System.Threading.Tasks;

internal static class CleanupScheduler
{
    private static System.Threading.Timer? _timer;
    private static int _running = 0;

    public static event EventHandler<(bool ok, string info)>? CleanupFinished;

    public static void ApplyFromSettings()
    {
        Stop();
        var s = SettingsStore.Current;
        if (!s.Enabled) return;
        if (!s.CleanupDailyEnabled && !s.CleanupEveryEnabled) return;

        if (s.CleanupDailyEnabled) ScheduleNextDaily();
        else if (s.CleanupEveryEnabled) ScheduleEveryMinutes();
    }

    public static void Stop()
    {
        var t = Interlocked.Exchange(ref _timer, null);
        t?.Dispose();
    }

    private static void ScheduleNextDaily()
    {
        var s = SettingsStore.Current;
        var now = DateTime.Now;
        var next = new DateTime(now.Year, now.Month, now.Day, s.CleanupDailyHour, s.CleanupDailyMinute, 0);
        if (next <= now) next = next.AddDays(1);

        var due = next - now;
        if (due < TimeSpan.Zero) due = TimeSpan.Zero;

        _timer = new System.Threading.Timer(_ => { _ = RunCleanupOnceAsync(rescheduleDaily: true); },
            null, due, Timeout.InfiniteTimeSpan);
    }

    private static void ScheduleEveryMinutes()
    {
        var s = SettingsStore.Current;
        var minutes = s.CleanupEveryMinutes;
        if (minutes < 1) minutes = 1;

        var period = TimeSpan.FromMinutes(minutes);
        _timer = new System.Threading.Timer(_ => { _ = RunCleanupOnceAsync(rescheduleDaily: false); },
            null, period, period);
    }

    private static async Task RunCleanupOnceAsync(bool rescheduleDaily)
    {
        if (Interlocked.Exchange(ref _running, 1) == 1) return;

        try
        {
            var s = SettingsStore.Current;
            if (!s.Enabled) return;
            if (!s.CleanupDailyEnabled && !s.CleanupEveryEnabled) return;

            var (ok, info) = await CleanupApi.DeleteInboxAllAsync();
            CleanupFinished?.Invoke(null, (ok, info));
        }
        catch (Exception ex)
        {
            CleanupFinished?.Invoke(null, (false, ex.Message));
        }
        finally
        {
            Interlocked.Exchange(ref _running, 0);

            if (rescheduleDaily)
            {
                var s = SettingsStore.Current;
                if (!s.Enabled) Stop();
                else if (s.CleanupDailyEnabled && !s.CleanupEveryEnabled) ScheduleNextDaily();
                else Stop();
            }
        }
    }
}