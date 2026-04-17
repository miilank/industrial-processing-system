public class PrimeProcessor : IJobProcessor
{
    public Task<int> ProcessAsync(Job job)
    {
        var parts = job.Payload.Split(',');
        int limit = int.Parse(parts[0].Split(':')[1].Replace("_", ""));
        int threads = int.Parse(parts[1].Split(':')[1]);
        threads = Math.Clamp(threads, 1, 8);

        return Task.Run(() =>
        {
            int count = 0;

            Parallel.For(2, limit + 1,
                new ParallelOptions { MaxDegreeOfParallelism = threads },
                i =>
                {
                    if (IsPrime(i))
                        Interlocked.Increment(ref count);
                });

            return count;
        });
    }

    private static bool IsPrime(int n)
    {
        if (n < 2) return false;
        for (int i = 2; i <= Math.Sqrt(n); i++)
            if (n % i == 0) return false;
        return true;
    }
}