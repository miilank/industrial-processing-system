using System.Xml.Linq;

public class ReportData
{
    public DateTime GeneratedAt { get; set; }
    public Dictionary<JobType, int> CompletedByType { get; set; } = new();
    public Dictionary<JobType, double> AvgElapsedByType { get; set; } = new();
    public Dictionary<JobType, int> FailedByType { get; set; } = new();
}

public class ReportGenerator
{
    private readonly List<(Job Job, int Result, double ElapsedMs, bool Failed)> _completed = new();
    private readonly object _lock = new();

    private int _reportIndex = 0;
    private const int MaxReports = 10;
    private readonly string _reportDir;

    public ReportGenerator(string reportDir)
    {
        _reportDir = reportDir;
        Directory.CreateDirectory(reportDir);
    }

    public void RecordCompleted(Job job, int result, double elapsedMs, bool failed = false)
    {
        lock (_lock)
            _completed.Add((job, result, elapsedMs, failed));
    }

    public ReportData GenerateReport()
    {
        lock (_lock)
        {
            return new ReportData
            {
                GeneratedAt = DateTime.Now,

                CompletedByType = _completed
                    .GroupBy(x => x.Job.Type)
                    .ToDictionary(g => g.Key, g => g.Count()),

                AvgElapsedByType = _completed
                    .GroupBy(x => x.Job.Type)
                    .ToDictionary(g => g.Key, g => g.Average(x => x.ElapsedMs)),

                FailedByType = _completed
                    .Where(x => x.Failed)
                    .GroupBy(x => x.Job.Type)
                    .OrderBy(g => g.Key)
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }
    }

    public async Task SaveReportAsync(ReportData data)
    {
        string path = Path.Combine(_reportDir, $"report_{_reportIndex % MaxReports}.xml");
        _reportIndex++;

        var doc = new XDocument(
            new XElement("Report",
                new XAttribute("GeneratedAt", data.GeneratedAt.ToString("o")),
                new XElement("CompletedByType",
                    data.CompletedByType.Select(kv =>
                        new XElement("Entry",
                            new XAttribute("Type", kv.Key),
                            new XAttribute("Count", kv.Value)))),
                new XElement("AvgElapsedByType",
                    data.AvgElapsedByType.Select(kv =>
                        new XElement("Entry",
                            new XAttribute("Type", kv.Key),
                            new XAttribute("AvgMs", kv.Value.ToString("F2"))))),
                new XElement("FailedByType",
                    data.FailedByType.Select(kv =>
                        new XElement("Entry",
                            new XAttribute("Type", kv.Key),
                            new XAttribute("Count", kv.Value))))
            )
        );

        await Task.Run(() => doc.Save(path));
        Console.WriteLine($"[Report] Saved: {path}");
    }

    public void StartPeriodicReporting(CancellationToken token)
    {
        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), token);
                    var report = GenerateReport();
                    await SaveReportAsync(report);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex) { Console.WriteLine($"[Report error] {ex.Message}"); }
            }
        }, token);
    }
}
