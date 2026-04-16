try
{
    var config = SystemConfig.Load("SystemConfig.xml");

    if (config != null)
    {
        Console.WriteLine($"Workers: {config.WorkerCount}");
        Console.WriteLine($"Queue Size: {config.MaxQueueSize}");

        if (config.InitialJobs != null)
        {
            Console.WriteLine($"Jobs loaded: {config.InitialJobs.Count}");

            foreach (var job in config.InitialJobs)
            {
                Console.WriteLine($"{job.Id} | {job.Type} | Priority: {job.Priority} | {job.Payload}");
            }
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}