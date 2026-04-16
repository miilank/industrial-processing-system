using System.Collections.Concurrent;

public class ProcessingSystem
{
    private readonly PriorityQueue<Job, int> _queue = new();
    private readonly object _lock = new();
    private readonly int _maxQueueSize;
    private readonly HashSet<Guid> _seenIds = new();
    private readonly ConcurrentDictionary<Guid, Job> _allJobs = new();
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<int>> _handles = new();
    public event EventHandler<JobCompletedEventArgs>? JobCompleted;
    public event EventHandler<JobFailedEventArgs>? JobFailed;
    private readonly Dictionary<JobType, IJobProcessor> _processors;

    public ProcessingSystem(SystemConfig config)
    {
        _maxQueueSize = config.MaxQueueSize;
        _processors = new Dictionary<JobType, IJobProcessor>
        {
            { JobType.Prime, new PrimeProcessor() },
            { JobType.IO,    new IoProcessor()    }
        };
    }

    public JobHandle? Submit(Job job)
    {
        lock (_lock)
        {
            if (_seenIds.Contains(job.Id))
                return null;

            if (_queue.Count >= _maxQueueSize)
                return null;

            _seenIds.Add(job.Id);
            _allJobs[job.Id] = job;
            _queue.Enqueue(job, job.Priority);
        }

        var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        _handles[job.Id] = tcs;

        return new JobHandle { Id = job.Id, Result = tcs.Task };
    }

    public async Task ProcessNextAsync()
    {
        if (!TryDequeue(out var job))
        {
            await Task.Delay(50);
            return;
        }

        const int maxAttempts = 3;
        const int timeoutMs = 2000;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var processTask = _processors[job.Type].ProcessAsync(job);
                var timeoutTask = Task.Delay(timeoutMs);

                if (await Task.WhenAny(processTask, timeoutTask) == timeoutTask)
                    throw new TimeoutException($"Job {job.Id} timed out on attempt {attempt}.");

                int result = await processTask;

                _handles[job.Id].SetResult(result);
                OnJobCompleted(job, result);
                return;
            }
            catch (Exception ex)
            {
                if (attempt == maxAttempts)
                {
                    _handles[job.Id].SetException(ex);
                    OnJobFailed(job, ex, aborted: true);
                }
            }
        }
    }

    private void OnJobCompleted(Job job, int result)
    {
        JobCompleted?.Invoke(this, new JobCompletedEventArgs
        {
            JobId = job.Id,
            Result = result
        });
    }

    private void OnJobFailed(Job job, Exception ex, bool aborted = true)
    {
        JobFailed?.Invoke(this, new JobFailedEventArgs
        {
            JobId = job.Id,
            Exception = ex,
            Aborted = aborted
        });
    }

    private bool TryDequeue(out Job job)
    {
        lock (_lock)
        {
            return _queue.TryDequeue(out job, out _);
        }
    }
}