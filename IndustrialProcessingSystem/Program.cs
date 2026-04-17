var config = SystemConfig.Load("SystemConfig.xml");
var system = new ProcessingSystem(config);
var logger = new EventLogger("events.log");
var reporter = new ReportGenerator("reports");
var pool = new WorkerPool(system, config.WorkerCount);

system.Reporter = reporter;
logger.Subscribe(system);

foreach (var job in config.InitialJobs)
    system.Submit(job);

pool.Start();

var cts = new CancellationTokenSource();
reporter.StartPeriodicReporting(cts.Token);

var random = new Random();
var jobTypes = new[] { JobType.Prime, JobType.IO };

var producers = Enumerable.Range(0, config.WorkerCount)
    .Select(_ => Task.Run(() =>
    {
        while (!cts.Token.IsCancellationRequested)
        {
            try
            {
                var type = jobTypes[random.Next(jobTypes.Length)];

                string payload = type == JobType.Prime
                    ? $"numbers:{random.Next(1_000, 50_000)},threads:{random.Next(1, 9)}"
                    : $"delay:{random.Next(100, 3_000)}";

                var job = new Job
                {
                    Id = Guid.NewGuid(),
                    Type = type,
                    Payload = payload,
                    Priority = random.Next(1, 5)
                };

                var handle = system.Submit(job);
                if (handle == null)
                    Thread.Sleep(100);
                else
                    Thread.Sleep(random.Next(100, 500));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Producer error] {ex.Message}");
            }
        }
    }))
    .ToArray();

Console.WriteLine($"System running - {config.WorkerCount} workers, max queue {config.MaxQueueSize}.");
Console.WriteLine("Press Enter to stop.");
Console.ReadLine();

cts.Cancel();
pool.Stop();
await Task.WhenAll(producers);
Console.WriteLine("System stopped.");