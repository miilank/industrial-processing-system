public class ProcessingSystem
{
    private readonly PriorityQueue<Job, int> _queue = new();
    // Lower priority value is dequeued first (smaller number = higher priority) which means
    // Jobs with smaller priority values are processed first

    // In PriorityQueue, the first element (root) is always the smallest (highest priority).
    // When dequeued, the system doesn't search the whole list; it uses a Binary Heap
    // (complete(every row is full before adding to new one from left to right) binary tree
    // where parrent is smaller than child, called min Heap, implemented as an array)
    // to quickly rebalance by adding the rightmost child to the root and rebalance.
    // Accessing the top is O(1), but Dequeue operation costs O(log n) due to rebalancing.
    // This is much faster than O(n log n) which would be the cost of sorting the entire list.

    private readonly object _lock = new();
    // Every object in .NET has a header that contains a Sync Block Index
    // (Type Pointer is stored too - contains metadata about the class/type. 
    // Fields are stored too - actual values of the object's instance data.)
    // When used in a lock, the object keeps track(written in index) of which thread currently holds the lock.
    // when _lock is locked, one thread runs that code at a time

    private readonly int _maxQueueSize;

    public ProcessingSystem(SystemConfig config)
    {
        _maxQueueSize = config.MaxQueueSize;
    }

    private bool TryEnqueue(Job job)
    {
        lock (_lock)
        {
            if (_queue.Count >= _maxQueueSize)
                return false;

            _queue.Enqueue(job, job.Priority);
            return true;
        }
    }

    private bool TryDequeue(out Job job)
    {
        lock (_lock)
        {
            return _queue.TryDequeue(out job, out _);
        }
    }
}