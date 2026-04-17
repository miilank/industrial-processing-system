public class EventLogger
{
    private readonly string _logPath;

    // problem: logger doesn't have its threads(logger uses workers threads). n workers can finish the job at the same time and
    // everyone tries to write in the file at that time
    private readonly SemaphoreSlim _semaphore = new(1, 1);// only one can write at a time(how many threads, max number of threads)

    public EventLogger(string logPath)
    {
        _logPath = logPath;
    }

    /*
        Logger subscribed and runs on worker threads, it fills the list of functions via += in Subscribe, 
        when Invoke is called all functions added with += are called, and then the lambda executes which writes 
        to the file - one at a time, controlled by the semaphore.
     */
    public void Subscribe(ProcessingSystem system)
    {
        system.JobCompleted += async (_, e) =>
            await WriteAsync($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [COMPLETED] {e.JobId}, {e.Result}");

        system.JobFailed += async (_, e) =>
            await WriteAsync($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{(e.Aborted ? "ABORT" : "FAILED")}] {e.JobId}, -1");
    }

    private async Task WriteAsync(string line)
    {
        await _semaphore.WaitAsync(); // thread is freed when waiting to do something else
        try
        {
            await File.AppendAllTextAsync(_logPath, line + Environment.NewLine);
        }
        finally // if exception is thrown when writing to the file, without finally, release would not be called because semaphore would be locked forever
        {
            _semaphore.Release();
        }
    }
}