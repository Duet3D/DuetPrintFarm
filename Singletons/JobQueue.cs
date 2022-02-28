using DuetPrintFarm.Model;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
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
        /// Wait for everything to be ready
        /// </summary>
        /// <returns>Asynchronous task</returns>
        public Task WaitToBeReady(CancellationToken cancellationToken);

        /// <summary>
        /// Called to flag full initialization
        /// </summary>
        public void SetReady();

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
        /// <returns>Asynchronous task</returns>
        public void Enqueue(Job job);

        /// <summary>
        /// Find a job that is still mapped to this machine
        /// </summary>
        /// <param name="hostname"></param>
        /// <returns>Job or null</returns>
        public Job FindJob(string hostname);

        /// <summary>
        /// Try to dequeue a pending job returning true on success
        /// </summary>
        /// <param name="job">Dequeued job</param>
        /// <returns>Whether a job could be dequeued</returns>
        public bool TryDequeue(out Job job);

        /// <summary>
        /// Dequeue a pending job
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Next pending job</returns>
        public ValueTask<Job> DequeueAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Pause a job being processed
        /// </summary>
        /// <param name="index">Index of the job to pause</param>
        /// <returns>Whether the job could be paused</returns>
        public bool Pause(int index);

        /// <summary>
        /// Resume a paused job
        /// </summary>
        /// <param name="index">Index of the job to resume</param>
        /// <returns>Whether the job could be resumed</returns>
        public bool Resume(int index);

        /// <summary>
        /// Cancel a paused job
        /// </summary>
        /// <param name="index">Index of the job to cancel</param>
        /// <returns>Whether the job could be cancelled</returns>
        public bool Cancel(int index);

        /// <summary>
        /// Repeat a processed job
        /// </summary>
        /// <param name="index">Index of the job to remove</param>
        public bool Repeat(int index);

        /// <summary>
        /// Remove an existing job
        /// </summary>
        /// <param name="index">Index of the job to remove</param>
        public bool Remove(int index);

        /// <summary>
        /// Remove printed jobs
        /// </summary>
        public void Clean();

        /// <summary>
        /// Called when a print has finished successfully
        /// </summary>
        /// <param name="job">Finished job</param>
        public void PrintFinished(Job job);

        /// <summary>
        /// Delegate for job events
        /// </summary>
        /// <param name="job">Affected job</param>
        public delegate void JobEvent(Job job);

        /// <summary>
        /// Called when a job is to be paused
        /// </summary>
        public event JobEvent OnPauseJob;

        /// <summary>
        /// Called when a job is to be resumed
        /// </summary>
        public event JobEvent OnResumeJob;

        /// <summary>
        /// Called when a job is to be cancelled
        /// </summary>
        public event JobEvent OnCancelJob;
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
        /// Monitor for concurrent access to the print queue and for notifying waiting clients about new print jobs
        /// </summary>
        private readonly AsyncMonitor _monitor = new();

        /// <summary>
        /// Lock access to the job queue
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Disposable lock</returns>
        public AwaitableDisposable<IDisposable> LockAsync(CancellationToken cancellationToken = default) => _monitor.EnterAsync(cancellationToken);

        /// <summary>
        /// Event to be set when everything is ready
        /// </summary>
        private readonly AsyncManualResetEvent _readyEvent = new();

        /// <summary>
        /// Wait for everything to be ready
        /// </summary>
        /// <returns>Asynchronous task</returns>
        public Task WaitToBeReady(CancellationToken cancellationToken) => _readyEvent.WaitAsync(cancellationToken);

        /// <summary>
        /// Mark everything ready
        /// </summary>
        public void SetReady() => _readyEvent.Set();

        /// <summary>
        /// List of all queued and running jobs
        /// </summary>
        private List<Job> _jobList = new();

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
            await using FileStream fs = new(filename, FileMode.Create, FileAccess.Write);
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
            await using (FileStream fs = new(filename, FileMode.Open, FileAccess.Read))
            {
                _jobList = await JsonSerializer.DeserializeAsync<List<Job>>(fs, cancellationToken: cancellationToken);
            }

            foreach (Job job in _jobList)
            {
                if (job.Hostname == null && job.TimeCompleted == null)
                {
                    job.Reset();
                    if (job.IsReadyToPrint)
                    {
                        _monitor.Pulse();
                    }
                }
            }
        }

        /// <summary>
        /// Enqueue a new or existing job
        /// </summary>
        /// <param name="job">Job to enqueue</param>
        /// <returns>Asynchronous task</returns>
        public void Enqueue(Job job)
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

                    bool jobInserted = false;
                    for (int i = 0; i < _jobList.Count; i++)
                    {
                        if (_jobList[i].Hostname == null || _jobList[i].TimeCompleted != null)
                        {
                            _jobList.Insert(i, job);
                            jobInserted = true;
                            break;
                        }
                    }
                    if (!jobInserted)
                    {
                        _jobList.Insert(0, job);
                    }
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

            _monitor.Pulse();
        }

        /// <summary>
        /// Find a job that is still mapped to this machine
        /// </summary>
        /// <param name="hostname"></param>
        /// <returns>Job or null</returns>
        public Job FindJob(string hostname)
        {
            foreach (Job item in _jobList)
            {
                if (hostname == item.Hostname && !item.Paused && !item.Cancelled && item.TimeCompleted == null)
                {
                    return item;
                }
            }
            return null;
        }

        /// <summary>
        /// Try to dequeue a pending job returning true on success
        /// </summary>
        /// <param name="job">Dequeued job</param>
        /// <returns>Whether a job could be dequeued</returns>
        public bool TryDequeue(out Job job)
        {
            foreach (Job item in _jobList)
            {
                if (item.IsReadyToPrint)
                {
                    job = item;
                    return true;
                }
            }
            job = null;
            return false;
        }

        /// <summary>
        /// Dequeue a pending job
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Next pending job</returns>
        public async ValueTask<Job> DequeueAsync(CancellationToken cancellationToken = default)
        {
            Job job;
            while (!TryDequeue(out job))
            {
                await _monitor.WaitAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
            }
            return job;
        }

        /// <summary>
        /// Called when a job is to be paused
        /// </summary>
        public event IJobQueue.JobEvent OnPauseJob;

        /// <summary>
        /// Pause a job being processed
        /// </summary>
        /// <param name="index">Index of the job to pause</param>
        /// <returns>Whether the job could be paused</returns>
        public bool Pause(int index)
        {
            if (index >= 0 && index < _jobList.Count)
            {
                lock (_jobList[index])
                {
                    if (_jobList[index].TimeCompleted == null && _jobList[index].Progress != null)
                    {
                        OnPauseJob?.Invoke(_jobList[index]);
                        return true;
                    }
                }
            }
            _logger.LogWarning("Failed to pause job #{0}", index);
            return false;
        }

        /// <summary>
        /// Called when a job is to be resumed
        /// </summary>
        public event IJobQueue.JobEvent OnResumeJob;

        /// <summary>
        /// Resume a paused job
        /// </summary>
        /// <param name="index">Index of the job to resume</param>
        /// <returns>Whether the job could be resumed</returns>
        public bool Resume(int index)
        {
            if (index >= 0 && index < _jobList.Count)
            {
                lock (_jobList[index])
                {
                    if (_jobList[index].Paused)
                    {
                        OnResumeJob?.Invoke(_jobList[index]);
                        return true;
                    }
                }
            }
            _logger.LogWarning("Failed to resume job #{0}", index);
            return false;
        }

        /// <summary>
        /// Called when a job is to be cancelled
        /// </summary>
        public event IJobQueue.JobEvent OnCancelJob;

        /// <summary>
        /// Cancel a paused job
        /// </summary>
        /// <param name="index">Index of the job to cancel</param>
        /// <returns>Whether the job could be cancelled</returns>
        public bool Cancel(int index)
        {
            if (index >= 0 && index < _jobList.Count)
            {
                lock (_jobList[index])
                {
                    if (_jobList[index].Paused)
                    {
                        OnCancelJob?.Invoke(_jobList[index]);
                        return true;
                    }
                }
            }
            _logger.LogWarning("Failed to cancel job #{0}", index);
            return false;
        }

        /// <summary>
        /// Repeat a processed job
        /// </summary>
        /// <param name="index">Index of the job to repeat</param>
        /// <returns>Whether the job could be repeated</returns>
        public bool Repeat(int index)
        {
            if (index >= 0 && index < _jobList.Count && _jobList[index].TimeCompleted != null)
            {
                Job job = _jobList[index];
                if (job.TimeCompleted != null)
                {
                    Enqueue(job);

                    _logger.LogInformation("Repeating job #{0}", index);
                    return true;
                }
            }
            _logger.LogWarning("Failed to repeat job #{0}", index);
            return false;
        }

        /// <summary>
        /// Remove an existing job
        /// </summary>
        /// <param name="index">Index of the job to remove</param>
        /// <returns>Whether the job could be removed</returns>
        public bool Remove(int index)
        {
            if (index >= 0 && index < _jobList.Count)
            {
                string jobFile = _jobList[index].AbsoluteFilename;
                _jobList.RemoveAt(index);
                TryRemoveJobFile(jobFile);

                _logger.LogInformation("Removed job #{0}", index);
                return true;
            }
            _logger.LogWarning("Failed to remove job #{0}", index);
            return false;
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
