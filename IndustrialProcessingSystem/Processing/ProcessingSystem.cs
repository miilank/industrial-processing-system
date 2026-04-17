using System.Collections.Concurrent;

public class ProcessingSystem
{
    private readonly PriorityQueue<Job, int> _queue = new();
    private readonly object _lock = new();
    private readonly int _maxQueueSize;

    private readonly HashSet<Guid> _seenIds = new();
    private readonly ConcurrentDictionary<Guid, Job> _allJobs = new(); // exists just because of GetJob(id)
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<int>> _handles = new(); // links producer and worker

    // list of functions that should be called if something happens
    // when logger does +=, function is added to the list
    // when worker calls Invoke, he calls every function in the list
    public event EventHandler<JobCompletedEventArgs>? JobCompleted; 
    public event EventHandler<JobFailedEventArgs>? JobFailed;

    public ReportGenerator? Reporter { get; set; }
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

    // put the job in the queue and send promise to producer that the Result will be sent when worker gets the job done
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

        // make the box for the result
        // when you fill the box, don't wait for the producer, let him wake up on his own thread
        // So worker and producer work independently, they don't block each other. 
        var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously); // can set the result
        _handles[job.Id] = tcs; // worker later finds this box(through handles) and puts the Result in it

        // tcs.Task is sent to producer so he can await the Result when worker fills the box
        return new JobHandle { Id = job.Id, Result = tcs.Task }; // can read the result if someone calls SetResult
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
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var processTask = _processors[job.Type].ProcessAsync(job);
                var timeoutTask = Task.Delay(timeoutMs);

                if (await Task.WhenAny(processTask, timeoutTask) == timeoutTask)
                    // wait for whatever Task finishes first, if timeout finishes first, the job took too long
                    throw new TimeoutException($"Job {job.Id} timed out on attempt {attempt}.");

                int result = await processTask;
                stopwatch.Stop();
                _handles[job.Id].SetResult(result); // fill the box, producer wakes up
                OnJobCompleted(job, result); // trigger the event -> logger writes to the file
                Reporter?.RecordCompleted(job, result, stopwatch.Elapsed.TotalMilliseconds, failed: false);
                return;
            }
            catch (Exception ex)
            {
                if (attempt < maxAttempts)
                {
                    // every failed attempt - FAILED
                    OnJobFailed(job, ex, aborted: false);
                }
                else
                {
                    // third attempt - ABORT
                    stopwatch.Stop();
                    _handles[job.Id].SetException(ex);
                    OnJobFailed(job, ex, aborted: true);
                    Reporter?.RecordCompleted(job, -1, stopwatch.Elapsed.TotalMilliseconds, failed: true);
                }
            }
        }
    }

    /*
        Logger subscribed and runs on worker threads, it fills the list of functions via += in Subscribe, 
        when Invoke is called all functions added with += are called, and then the lambda executes which writes 
        to the file — one at a time, controlled by the semaphore.
     */
    private void OnJobCompleted(Job job, int result)
    {
        JobCompleted?.Invoke(this, new JobCompletedEventArgs // Invoke calls every lambda that is added by +=
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

    public IEnumerable<Job> GetTopJobs(int n)
    {
        lock (_lock)
        {
            return _queue.UnorderedItems
                .OrderBy(x => x.Priority)
                .Take(n)
                .Select(x => x.Element)
                .ToList();
        }
    }

    public Job? GetJob(Guid id)
    {
        _allJobs.TryGetValue(id, out var job);
        return job;
    }
}