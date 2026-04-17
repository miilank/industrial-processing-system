public class IoProcessor : IJobProcessor
{
    private static readonly Random _rng = new();
    public Task<int> ProcessAsync(Job job)
    {
        int delay = int.Parse(job.Payload.Split(':')[1].Replace("_", ""));
        return Task.Run(() =>
        {
            Thread.Sleep(delay);
            return _rng.Next(0, 101);
        });
    }
}