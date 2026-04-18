## Industrial Processing System

A thread-safe industrial job processing service with a priority queue, async execution, and event-driven architecture.

The system loads its configuration from SystemConfig.xml which defines the number of worker threads, the maximum queue size, and an initial set of jobs. After startup, producer threads continuously generate and submit new jobs on their own.

### Job Types

Prime jobs count prime numbers up to a given limit using parallel computation. Thread count is clamped to 1-8.

IO jobs simulate a read operation by sleeping for a given number of milliseconds and returning a random number between 0 and 100.

### How It Works

Producer threads submit jobs to a thread-safe priority queue. Lower priority number means higher priority. Jobs are rejected if the queue is full or if the same ID has already been submitted.

Worker threads pull jobs from the queue and process them with a 2 second timeout. Each job is attempted up to 3 times total. If all attempts fail the job is aborted.

Results are delivered through a TaskCompletionSource. The caller gets a JobHandle with a Task that resolves when the job finishes. Completed and failed jobs are logged asynchronously to events.log.

Every 60 seconds a LINQ report is written to an XML file in the reports directory covering completed jobs per type, average execution time per type, and failed jobs grouped by type. The last 10 reports are kept, oldest overwritten first.
