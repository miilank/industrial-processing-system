using System.Xml.Linq;

public class SystemConfig
{
    public int WorkerCount { get; set; }
    public int MaxQueueSize { get; set; }
    public List<Job> InitialJobs { get; set; } = new();

    public static SystemConfig Load(string path)
    {
        var doc = XDocument.Load(path);
        var root = doc.Root;

        if (root == null)
            throw new Exception("XML Error: Root element 'SystemConfig' not found.");

        var config = new SystemConfig();

        var workerElem = root.Element("WorkerCount");
        if (workerElem == null) throw new Exception("Missing 'WorkerCount' in config.");
        config.WorkerCount = int.Parse(workerElem.Value);

        var queueElem = root.Element("MaxQueueSize");
        if (queueElem == null) throw new Exception("Missing 'MaxQueueSize' in config.");
        config.MaxQueueSize = int.Parse(queueElem.Value);

        var jobsElem = root.Element("Jobs");
        if (jobsElem != null)
        {
            config.InitialJobs = jobsElem.Elements("Job").Select(j =>
            {
                var typeAttr = j.Attribute("Type") ?? throw new Exception("Job Type missing.");
                var priorityAttr = j.Attribute("Priority") ?? throw new Exception("Job Priority missing.");
                var payloadAttr = j.Attribute("Payload") ?? throw new Exception("Job Payload missing.");

                return new Job
                {
                    Id = Guid.NewGuid(),
                    Type = Enum.Parse<JobType>(typeAttr.Value),
                    Payload = payloadAttr.Value,
                    Priority = int.Parse(priorityAttr.Value)
                };
            }).ToList();
        }

        return config;
    }
}