using DuetPrintFarm.Model;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DuetPrintFarm.Singletons
{
    /// <summary>
    /// Interface for accessing the job queue
    /// </summary>
    public interface IJobQueue
    {
        /// <summary>
        /// Lock access to the job queue
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Disposable lock</returns>
        public AwaitableDisposable<IDisposable> LockAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all jobs as JSON
        /// </summary>
        /// <returns>JSON array</returns>
        public string ToJson();

        /// <summary>
        /// Save the job queue to a given file
        /// </summary>
        /// <param name="filename">Filename containing the job queue</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Asynchronous task</returns>
        public Task SaveToFileAsync(string filename, CancellationToken cancellationToken = default);

        /// <summary>
        /// Load the job queue from a given file
        /// </summary>
        /// <param name="filename">Filename containing the job queue</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Asynchronous task</returns>
        public Task LoadFromFileAsync(string filename, CancellationToken cancellationToken = default);

        /// <summary>
        /// Enqueue a new or existing job
        /// </summary>
        /// <param name="job">Job to enqueue</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Asynchronous task</returns>
        public Task EnqueueAsync(Job job, CancellationToken cancellationToken = default);

        /// <summary>
        /// Try to dequeue a pending job returning true on success
        /// </summary>
        /// <param name="job">Dequeued job</param>
        /// <returns>Whether a job could be dequeued</returns>
        public bool TryDequeue(out Job job);

        /// <summary>
        /// Dequeue a pending job
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>Next pending job</returns>
        public ValueTask<Job> DequeueAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Pause a job being processed
        /// </summary>
        /// <param name="job">Job to pause</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Asynchronous task</returns>
        public Task PauseAsync(string filename, CancellationToken cancellationToken = default);

        /// <summary>
        /// Pause a job being processed
        /// </summary>
        /// <param name="index">Index of the job to pause</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Asynchronous task</returns>
        public Task PauseAsync(int index, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resume a paused job
        /// </summary>
        /// <param name="job">Job to resume</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Asynchronous task</returns>
        public Task ResumeAsync(string filename, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resume a paused job
        /// </summary>
        /// <param name="index">Index of the job to resume</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Asynchronous task</returns>
        public Task ResumeAsync(int index, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancel a paused job
        /// </summary>
        /// <param name="job">Job to cancel</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Asynchronous task</returns>
        public Task CancelAsync(string filename, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancel a paused job
        /// </summary>
        /// <param name="index">Index of the job to cancel</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Asynchronous task</returns>
        public Task CancelAsync(int index, CancellationToken cancellationToken = default);

        /// <summary>
        /// Repeat a processed job
        /// </summary>
        /// <param name="job">Job to remove</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Asynchronous task</returns>
        public Task RepeatAsync(string filename, CancellationToken cancellationToken = default);

        /// <summary>
        /// Repeat a processed job
        /// </summary>
        /// <param name="index">Index of the job to remove</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Asynchronous task</returns>
        public Task RepeatAsync(int index, CancellationToken cancellationToken = default);

        /// <summary>
        /// Remove an existing job
        /// </summary>
        /// <param name="job">Job to remove</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Asynchronous task</returns>
        public Task RemoveAsync(string filename, CancellationToken cancellationToken = default);

        /// <summary>
        /// Remove an existing job
        /// </summary>
        /// <param name="index">Index of the job to remove</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Asynchronous task</returns>
        public Task RemoveAsync(int index, CancellationToken cancellationToken = default);

        /// <summary>
        /// Remove printed jobs
        /// </summary>
        public void Clean();

        /// <summary>
        /// Called when a print has finished successfully
        /// </summary>
        /// <param name="job">Finished job</param>
        public void PrintFinished(Job job);
    }

    /// <summary>
    /// Singleton class for providing the job queue
    /// </summary>
    public class JobQueue : IJobQueue
    {
        /// <summary>
        /// Logger instance
        /// </summary>
        private readonly ILogger<JobQueue> _logger;

        /// <summary>
        /// Constructor of this class
        /// </summary>
        /// <param name="logger">Logger instance</param>
        public JobQueue(ILogger<JobQueue> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Lock for concurrent access to the print queue
        /// </summary>
        private readonly AsyncLock _lock = new();

        /// <summary>
        /// Lock access to the job queue
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Disposable lock</returns>
        public AwaitableDisposable<IDisposable> LockAsync(CancellationToken cancellationToken = default) => _lock.LockAsync(cancellationToken);

        /// <summary>
        /// List of all queued and running jobs
        /// </summary>
        public List<Job> _jobList = new();

        /// <summary>
        /// List of pending jobs. There is only a single writer because this instance is locked whenever it is written to
        /// </summary>
        private readonly Channel<Job> _pendingJobs = Channel.CreateUnbounded<Job>(new UnboundedChannelOptions() { SingleWriter = true });

        /// <summary>
        /// Get all jobs as JSON
        /// </summary>
        /// <returns>JSON array</returns>
        public string ToJson()
        {
            using MemoryStream jsonStream = new();

            // Write JSON
            using (Utf8JsonWriter writer = new(jsonStream))
            {
                writer.WriteStartArray();
                foreach (Job job in _jobList)
                {
                    lock (job)
                    {
                        JsonSerializer.Serialize(writer, job);
                    }
                }
                writer.WriteEndArray();
            }

            // Get it as a string
            using StreamReader reader = new(jsonStream, Encoding.UTF8);
            jsonStream.Seek(0, SeekOrigin.Begin);
            return reader.ReadToEnd();
        }

        /// <summary>
        /// Save the job queue to a given file
        /// </summary>
        /// <param name="filename">Filename containing the job queue</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Asynchronous task</returns>
        public async Task SaveToFileAsync(string filename, CancellationToken cancellationToken = default)
        {
            using FileStream fs = new(filename, FileMode.Create, FileAccess.Write);
            await JsonSerializer.SerializeAsync(fs, _jobList, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Load the job queue from a given file
        /// </summary>
        /// <param name="filename">Filename containing the job queue</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Asynchronous task</returns>
        public async Task LoadFromFileAsync(string filename, CancellationToken cancellationToken = default)
        {
            using (FileStream fs = new(filename, FileMode.Open, FileAccess.Read))
            {
                _jobList = await JsonSerializer.DeserializeAsync<List<Job>>(fs, cancellationToken: cancellationToken);
            }

            foreach (Job job in _jobList)
            {
                if (job.TimeCompleted == null)
                {
                    job.Reset();
                }
            }
            await RebuildPendingJobsAsync(cancellationToken);
        }

        /// <summary>
        /// Enqueue a new or existing job
        /// </summary>
        /// <param name="job">Job to enqueue</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Asynchronous task</returns>
        public async Task EnqueueAsync(Job job, CancellationToken cancellationToken = default)
        {
            lock (job)
            {
                job.Reset();
            }

            if (_jobList.Contains(job))
            {
                if (_jobList.Count > 1)
                {
                    _jobList.Remove(job);

                    for (int i = 0; i < _jobList.Count; i++)
                    {
                        if (_jobList[i].Hostname == null)
                        {
                            _jobList.Insert(i, job);
                            return;
                        }
                    }
                    _jobList.Insert(0, job);
                }
                _logger.LogInformation("Existing job {0} enqueued", Path.GetFileName(job.AbsoluteFilename));
            }
            else
            {
                bool jobAdded = false;
                for (int i = 0; i < _jobList.Count; i++)
                {
                    if (_jobList[i].TimeCompleted != null)
                    {
                        _jobList.Insert(i, job);
                        jobAdded = true;
                        break;
                    }
                }
                if (!jobAdded)
                {
                    _jobList.Add(job);
                }
                _logger.LogInformation("New job {0} enqueued", Path.GetFileName(job.AbsoluteFilename));
            }
            await RebuildPendingJobsAsync(cancellationToken);
        }

        /// <summary>
        /// Rebuild the pending job queue
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Asynchronous task</returns>
        private async Task RebuildPendingJobsAsync(CancellationToken cancellationToken = default)
        {
            while (_pendingJobs.Reader.TryRead(out _)) { }

            foreach (Job job in _jobList)
            {
                bool readyToPrint;
                lock (job)
                {
                    readyToPrint = job.Hostname == null && !job.Paused && job.TimeCompleted == null;
                }

                if (readyToPrint)
                {
                    await _pendingJobs.Writer.WriteAsync(job, cancellationToken);
                }
            }
        }

        /// <summary>
        /// Try to dequeue a pending job returning true on success
        /// </summary>
        /// <param name="job">Dequeued job</param>
        /// <returns>Whether a job could be dequeued</returns>
        public bool TryDequeue(out Job job) => _pendingJobs.Reader.TryRead(out job);

        /// <summary>
        /// Dequeue a pending job
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>Next pending job</returns>
        public ValueTask<Job> DequeueAsync(CancellationToken cancellationToken) => _pendingJobs.Reader.ReadAsync(cancellationToken);

        /// <summary>
        /// Pause a job being processed
        /// </summary>
        /// <param name="job">Job to pause</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Asynchronous task</returns>
        public async Task PauseAsync(string filename, CancellationToken cancellationToken = default)
        {
            foreach (Job job in _jobList)
            {
                if (job.Filename == filename && job.Progress != null && job.TimeCompleted == null)
                {
#warning Implement this

                    _logger.LogInformation("Paused job {0}", filename);
                    return;
                }
            }
            _logger.LogWarning("Failed to pause job {0}", filename);
        }

        /// <summary>
        /// Pause a job being processed
        /// </summary>
        /// <param name="index">Index of the job to pause</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Asynchronous task</returns>
        public async Task PauseAsync(int index, CancellationToken cancellationToken = default)
        {
            if (index >= 0 && index < _jobList.Count && _jobList[index].TimeCompleted == null && _jobList[index].Progress != null)
            {
#warning Implement this

                _jobList[index].Paused = true;
                _logger.LogInformation("Paused job #{0}", index);
                return;
            }
            _logger.LogWarning("Failed to pause job #{0}", index);
        }

        /// <summary>
        /// Resume a paused job
        /// </summary>
        /// <param name="job">Job to resume</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Asynchronous task</returns>
        public async Task ResumeAsync(string filename, CancellationToken cancellationToken = default)
        {
            foreach (Job job in _jobList)
            {
                if (job.Filename == filename && job.Progress != null && job.Paused)
                {
#warning Implement this

                    _logger.LogInformation("Resumed job {0}", filename);
                    return;
                }
            }
            _logger.LogWarning("Failed to resume job {0}", filename);
        }

        /// <summary>
        /// Resume a paused job
        /// </summary>
        /// <param name="index">Index of the job to resume</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Asynchronous task</returns>
        public async Task ResumeAsync(int index, CancellationToken cancellationToken = default)
        {
            if (index >= 0 && index < _jobList.Count && _jobList[index].Paused)
            {
#warning Implement this

                _jobList[index].Paused = false;
                _logger.LogInformation("Resumed job #{0}", index);
                return;
            }
            _logger.LogWarning("Failed to resume job #{0}", index);
        }


        /// <summary>
        /// Cancel a paused job
        /// </summary>
        /// <param name="job">Job to cancel</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Asynchronous task</returns>
        public async Task CancelAsync(string filename, CancellationToken cancellationToken = default)
        {
            foreach (Job job in _jobList)
            {
                if (job.Filename == filename && job.Progress != null && job.Paused)
                {
#warning Implement this

                    _logger.LogInformation("Cancelled job {0}", filename);
                    return;
                }
            }
            _logger.LogWarning("Failed to cancel job {0}", filename);
        }

        /// <summary>
        /// Cancel a paused job
        /// </summary>
        /// <param name="index">Index of the job to cancel</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Asynchronous task</returns>
        public async Task CancelAsync(int index, CancellationToken cancellationToken = default)
        {
            if (index >= 0 && index < _jobList.Count && _jobList[index].Paused)
            {
#warning Implement this

                _jobList[index].Paused = false;
                _jobList[index].Cancelled = true;
                _logger.LogInformation("Cancelled job #{0}", index);
                return;
            }
            _logger.LogWarning("Failed to cancel job #{0}", index);
        }

        /// <summary>
        /// Repeat a processed job
        /// </summary>
        /// <param name="job">Job to repeat</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Asynchronous task</returns>
        public async Task RepeatAsync(string filename, CancellationToken cancellationToken = default)
        {
            foreach (Job job in _jobList)
            {
                if (job.Filename == filename && job.TimeCompleted != null)
                {
                    await EnqueueAsync(job, cancellationToken);

                    _logger.LogInformation("Repeating job {0}", filename);
                    return;
                }
            }
            _logger.LogWarning("Failed to repeat job {0}", filename);
        }

        /// <summary>
        /// Repeat a processed job
        /// </summary>
        /// <param name="index">Index of the job to repeat</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Asynchronous task</returns>
        public async Task RepeatAsync(int index, CancellationToken cancellationToken = default)
        {
            if (index >= 0 && index < _jobList.Count && _jobList[index].TimeCompleted != null)
            {
                Job job = _jobList[index];
                if (job.TimeCompleted != null)
                {
                    await EnqueueAsync(job, cancellationToken);

                    _logger.LogInformation("Repeating job #{0}", index);
                    return;
                }
            }
            _logger.LogWarning("Failed to repeat job #{0}", index);
        }

        /// <summary>
        /// Remove an existing job
        /// </summary>
        /// <param name="job">Job to remove</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Asynchronous task</returns>
        public async Task RemoveAsync(string filename, CancellationToken cancellationToken = default)
        {
            foreach (Job job in _jobList)
            {
                if (job.Filename == filename)
                {
                    _jobList.Remove(job);
                    TryRemoveJobFile(job.AbsoluteFilename);
                    await RebuildPendingJobsAsync(cancellationToken);

                    _logger.LogInformation("Removed job {0}", filename);
                    return;
                }
            }
            _logger.LogWarning("Failed to remove job {0}", filename);
        }

        /// <summary>
        /// Remove an existing job
        /// </summary>
        /// <param name="index">Index of the job to remove</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Asynchronous task</returns>
        public async Task RemoveAsync(int index, CancellationToken cancellationToken = default)
        {
            if (index >= 0 && index < _jobList.Count)
            {
                string jobFile = _jobList[index].AbsoluteFilename;
                _jobList.RemoveAt(index);
                TryRemoveJobFile(jobFile);
                await RebuildPendingJobsAsync(cancellationToken);

                _logger.LogInformation("Removed job #{0}", index);
                return;
            }
            _logger.LogWarning("Failed to remove job #{0}", index);
        }

        /// <summary>
        /// Try to remove a job file in case it is no longer referenced
        /// </summary>
        /// <param name="filename">Filename to delete</param>
        private void TryRemoveJobFile(string filename)
        {
            if (File.Exists(filename))
            {
                foreach (Job job in _jobList)
                {
                    if (job.TimeCompleted != null && job.AbsoluteFilename == filename)
                    {
                        return;
                    }
                }
                File.Delete(filename);
            }
        }

        /// <summary>
        /// Remove finished jobs
        /// </summary>
        public void Clean()
        {
            for (int i = _jobList.Count - 1; i >= 0; i--)
            {
                if (_jobList[i].TimeCompleted != null)
                {
                    string jobFile = _jobList[i].AbsoluteFilename;
                    _jobList.RemoveAt(i);
                    TryRemoveJobFile(jobFile);
                }
            }
        }

        /// <summary>
        /// Called when a print has finished successfully
        /// </summary>
        /// <param name="job">Finished job</param>
        public void PrintFinished(Job job)
        {
            lock (job)
            {
                job.ProgressText = null;
                job.Progress = 1;
                job.TimeLeft = null;
                job.TimeCompleted = DateTime.Now;
            }

            _jobList.Remove(job);
            _jobList.Add(job);
        }
    }
}
