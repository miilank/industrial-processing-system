public class JobCompletedEventArgs : EventArgs
{
    public Guid JobId { get; set; }
    public int Result { get; set; }
}

public class JobFailedEventArgs : EventArgs
{
    public Guid JobId { get; set; }
    public Exception? Exception { get; set; }
    public bool Aborted { get; set; }
}