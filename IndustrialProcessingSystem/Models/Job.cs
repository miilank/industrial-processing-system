public class Job
{
    public Guid Id { get; set; }
    public JobType Type { get; init; }
    public string Payload { get; init; }
    public int Priority { get; set; }
}