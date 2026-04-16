public class IoProcessor : IJobProcessor
{
    private static readonly Random _rng = new();
    public Task<int> ProcessAsync(Job job)
    {
        int delay = int.Parse(job.Payload.Split(':')[1].Replace("_", ""));
        Thread.Sleep(delay);
        return Task.FromResult(_rng.Next(0, 101));
    }
}