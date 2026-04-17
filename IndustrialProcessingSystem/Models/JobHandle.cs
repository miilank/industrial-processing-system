public class JobHandle
{
    public Guid Id { get; set; }
    public Task<int> Result { get; set; } // task is type that represents async operation that returns int when the work completes
}