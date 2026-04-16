public class EventLogger
{
    private readonly string _logPath;

    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public EventLogger(string logPath)
    {
        _logPath = logPath;
    }

    public void Subscribe(ProcessingSystem system)
    {
        system.JobCompleted += async (_, e) =>
            await WriteAsync($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [COMPLETED] {e.JobId}, {e.Result}");

        system.JobFailed += async (_, e) =>
            await WriteAsync($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{(e.Aborted ? "ABORT" : "FAILED")}] {e.JobId}, -1");
    }

    private async Task WriteAsync(string line)
    {
        await _semaphore.WaitAsync();
        try
        {
            await File.AppendAllTextAsync(_logPath, line + Environment.NewLine);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}