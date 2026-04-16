public class WorkerPool
{
    private readonly ProcessingSystem _system;
    private readonly int _workerCount;
    private readonly CancellationTokenSource _cts = new();

    public WorkerPool(ProcessingSystem system, int workerCount)
    {
        _system = system;
        _workerCount = workerCount;
    }

    public void Start()
    {
        for (int i = 0; i < _workerCount; i++)
        {
            Task.Run(() => WorkerLoop(_cts.Token));
        }
    }

    private async Task WorkerLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await _system.ProcessNextAsync();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Worker error] {ex.Message}");
            }
        }
    }

    public void Stop() => _cts.Cancel();
}