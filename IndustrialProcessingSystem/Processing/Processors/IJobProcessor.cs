public interface IJobProcessor
{
    Task<int> ProcessAsync(Job job);
}