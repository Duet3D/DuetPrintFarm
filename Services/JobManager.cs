using DuetPrintFarm.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
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

namespace DuetPrintFarm.Services
{
    /// <summary>
    /// Main service to schedule jobs for the different printers
    /// </summary>
    public sealed class JobManager : IHostedService
    {
        /// <summary>
        /// Configuration of this application
        /// </summary>
        private readonly IConfiguration _configuration;

        /// <summary>
        /// File where the job queue is stored
        /// </summary>
        private string JobQueueFile { get => _configuration.GetValue<string>("JobQueueFile"); }

        /// <summary>
        /// Logger instance
        /// </summary>
        private static ILogger _logger;

        /// <summary>
        /// Lock for concurrent access to the print queue
        /// </summary>
        private static readonly AsyncLock _lock = new();

        /// <summary>
        /// Lock access to the current jobs
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Disposable lock</returns>
        public static AwaitableDisposable<IDisposable> LockAsync(CancellationToken cancellationToken = default) => _lock.LockAsync(cancellationToken);

        /// <summary>
        /// List of all queued and running jobs
        /// </summary>
        public static List<Job> Jobs { get; private set; } = new();

        /// <summary>
        /// Get all jobs as JSON
        /// </summary>
        /// <returns>JSON array</returns>
        public static string GetJson()
        {
            using MemoryStream jsonStream = new();

            // Write JSON
            using (Utf8JsonWriter writer = new(jsonStream))
            {
                writer.WriteStartArray();
                foreach (Job job in Jobs)
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
        /// List of pending jobs
        /// </summary>
        private static readonly Channel<Job> _pendingJobs = Channel.CreateUnbounded<Job>();

        /// <summary>
        /// Enqueue a new or existing job
        /// </summary>
        /// <param name="job"></param>
        /// <returns></returns>
        public static async Task Enqueue(Job job, CancellationToken cancellationToken = default)
        {
            lock (job)
            {
                job.Hostname = null;
            }

            using (await _lock.LockAsync(cancellationToken))
            {
                if (Jobs.Contains(job))
                {
                    _logger.LogInformation("Existing job {0} enqueued", Path.GetFileName(job.AbsoluteFilename));
                }
                else
                {
                    Jobs.Add(job);
                    _logger.LogInformation("New job {0} enqueued", Path.GetFileName(job.AbsoluteFilename));
                }
            }
            await _pendingJobs.Writer.WriteAsync(job, cancellationToken);
        }

        /// <summary>
        /// Dequeue a pending job
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>Next pending job</returns>
        public static ValueTask<Job> Dequeue(CancellationToken cancellationToken) => _pendingJobs.Reader.ReadAsync(cancellationToken);

        /// <summary>
        /// Remove an existing job
        /// </summary>
        /// <param name="job">Job to remove</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Asynchronous task</returns>
        public static async Task Remove(string filename, CancellationToken cancellationToken = default)
        {
            using (await _lock.LockAsync(cancellationToken))
            {
                foreach (Job job in Jobs)
                {
                    if (job.Filename == filename)
                    {
                        Jobs.Remove(job);
                        _logger.LogInformation("Removed job {0}", filename);
                        return;
                    }
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
        public static async Task Remove(int index, CancellationToken cancellationToken = default)
        {
            using (await _lock.LockAsync(cancellationToken))
            {
                if (index >= 0 && index < Jobs.Count)
                {
                    Jobs.RemoveAt(index);
                    _logger.LogInformation("Removed job #{0}", index);
                    return;
                }
            }
            _logger.LogWarning("Failed to remove job #{0}", index);
        }

        /// <summary>
        /// Constructor of this service class
        /// </summary>
        /// <param name="configuration">App configuration</param>
        /// <param name="logger">Logger instance</param>
        public JobManager(IConfiguration configuration, ILogger<JobManager> logger)
        {
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Start this service
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Asynchronous task</returns>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // Load the job queue if possible
            if (File.Exists(JobQueueFile))
            {
                using FileStream fs = new(JobQueueFile, FileMode.Open, FileAccess.Read);
                Jobs = await JsonSerializer.DeserializeAsync<List<Job>>(fs, cancellationToken: cancellationToken);
                foreach (Job job in Jobs)
                {
                    job.Progress = null;
                    job.TimeLeft = null;
                    if (job.TimeCompleted == null)
                    {
                        await Enqueue(job, cancellationToken);
                    }
                }
            }

            // Log this
            _logger.LogInformation("Job manager started");
        }

        /// <summary>
        /// Stop this service
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Asynchronous task</returns>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            // Save the current print queue
            using FileStream fs = new(JobQueueFile, FileMode.Create, FileAccess.Write);
            using (await _lock.LockAsync(cancellationToken))
            {
                await JsonSerializer.SerializeAsync(fs, Jobs, cancellationToken: cancellationToken);
            }

            // Log this
            _logger.LogInformation("Job manager stopped");
        }
    }
}
