using System.Collections.Concurrent;

public class ProcessingSystem
{
    private readonly PriorityQueue<Job, int> _queue = new();
    private readonly object _lock = new();
    private readonly int _maxQueueSize;
    private readonly HashSet<Guid> _seenIds = new();
    private readonly ConcurrentDictionary<Guid, Job> _allJobs = new();
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<int>> _handles = new();

    public ProcessingSystem(SystemConfig config)
    {
        _maxQueueSize = config.MaxQueueSize;
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

    private bool TryDequeue(out Job job)
    {
        lock (_lock)
        {
            return _queue.TryDequeue(out job, out _);
        }
    }
}