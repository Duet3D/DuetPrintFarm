using DuetAPI.ObjectModel;
using DuetHttpClient;
using DuetPrintFarm.Services;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using System;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace DuetPrintFarm.Model
{
    /// <summary>
    /// Class representing a remote printer
    /// </summary>
    public class Printer : IAsyncDisposable
    {
        /// <summary>
        /// Hostname of this printer
        /// </summary>
        public string Hostname { get; }

        /// <summary>
        /// Logger instance
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Remote session of this printer 
        /// </summary>
        private DuetHttpSession _session;

        /// <summary>
        /// Indicates if this machine is online
        /// </summary>
        public bool Online { get; private set; }

        /// <summary>
        /// Current job of this printer
        /// </summary>
        [JsonIgnore]
        public Job Job { get; set; }

        /// <summary>
        /// Current job file being processed (may or may not be a valid job)
        /// </summary>
        public string JobFile { get; private set; }

        /// <summary>
        /// Constructor of this class
        /// </summary>
        /// <param name="hostname">Hostname of this printer</param>
        public Printer(string hostname, ILogger logger)
        {
            Hostname = hostname;
            _logger = logger;
            SessionTask = Task.Run(MaintainSession);
        }

        /// <summary>
        /// CTS to be triggered when this instance is disposed
        /// </summary>
        private readonly CancellationTokenSource _disposedTCS = new();

        /// <summary>
        /// Whether this instance is disposed
        /// </summary>
        private bool disposed;

        /// <summary>
        /// Dispose this instance asynchronously
        /// </summary>
        /// <returns>Asynchronous task</returns>
        public async ValueTask DisposeAsync()
        {
            if (disposed)
            {
                return;
            }
            disposed = true;

            _disposedTCS.Cancel();
            if (_session != null)
            {
                await _session.DisposeAsync();
            }

            _disposedTCS.Dispose();
        }

        /// <summary>
        /// Task that is used to maintain this printer
        /// </summary>
        public readonly Task SessionTask;

        /// <summary>
        /// Method to keep the session 
        /// </summary>
        /// <returns>Asynchronous task</returns>
        private async Task MaintainSession()
        {
            // Establish a new session first
            do
            {
                try
                {
                    _session = await DuetHttpSession.ConnectAsync(new Uri($"http://{Hostname}"));
                }
                catch (Exception)
                {
                    _logger.LogWarning("Failed to connect to printer {0}", Hostname);
                    await Task.Delay(2000);
                }

                if (disposed)
                {
                    // Stop if the printer has been deleted
                    return;
                }
            }
            while (_session == null);

            // Keep the estimated times left and file progress up-to-date
            _session.Model.Job.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(ObjectModel.Job.Duration))
                {
                    long? printTime = _session.Model.Job.File.PrintTime ?? _session.Model.Job.File.SimulatedTime;
                    if (printTime != null)
                    {
                        lock (this)
                        {
                            if (Job != null)
                            {
                                lock (Job)
                                {
                                    if (Job.TimeCompleted == null && _session.Model.Job.Duration != null)
                                    {
                                        Job.TimeLeft = Math.Max(printTime.Value - _session.Model.Job.Duration.Value, 0);
                                    }
                                    else
                                    {
                                        Job.TimeLeft = null;
                                    }
                                }
                            }
                        }
                    }
                }
                else if (e.PropertyName == nameof(ObjectModel.Job.FilePosition))
                {
                    lock (this)
                    {
                        if (Job != null)
                        {
                            lock (Job)
                            {
                                if (Job.TimeCompleted == null && _session.Model.Job.FilePosition != null && _session.Model.Job.File.Size > 0)
                                {
                                    Job.Progress = (double)_session.Model.Job.FilePosition / _session.Model.Job.File.Size;
                                }
                                else
                                {
                                    Job.Progress = null;
                                }
                            }
                        }
                    }
                }
            };

            // Watch for job file changes
            AsyncManualResetEvent machineIdle = new(), machinePrinting = new();
            _session.Model.Job.File.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(ObjectModel.Job.File.FileName))
                {
                    if (_session.Model.Job.File.FileName == null)
                    {
                        // No longer printing
                        lock (this)
                        {
                            Job = null;
                            JobFile = null;
                            _logger.LogInformation("Printer {0} is no longer printing", Hostname);
                        }
                        machinePrinting.Reset();

#warning MachineStatus.off is not valid for real use-cases, but useful for bench setups
                        if (_session.Model.State.Status == MachineStatus.Off)
                        {
                            machineIdle.Set();
                        }
                    }
                    else
                    {
                        // Now printing
                        lock (this)
                        {
                            // Job is initially set by the job scheduler
                            JobFile = Path.GetFileName(_session.Model.Job.File.FileName);
                            _logger.LogInformation("Printer {0} is printing {1}", Hostname, JobFile);
                        }
                        machinePrinting.Set();

#warning MachineStatus.off is not valid for real use-cases, but useful for bench setups
                        if (_session.Model.State.Status == MachineStatus.Off)
                        {
                            machineIdle.Reset();
                        }
                    }
                }
            };

            // Watch for state changes
            _session.Model.State.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(ObjectModel.State.Status))
                {
                    bool isOnline = (_session.Model.State.Status != MachineStatus.Starting) && (_session.Model.State.Status != MachineStatus.Disconnected);
                    lock (this)
                    {
                        if (Online != isOnline)
                        {
                            Online = isOnline;
                            _logger.LogInformation("Printer {0} is now {1}", Hostname, isOnline ? "online" : "offline");
                        }
                    }

#warning MachineStatus.off is not valid for real use-cases, but useful for bench setups
                    if (_session.Model.State.Status == MachineStatus.Idle || (_session.Model.State.Status == MachineStatus.Off && _session.Model.Job.File.FileName == null))
                    {
                        machineIdle.Set();
                    }
                    else
                    {
                        machineIdle.Reset();
                    }
                }
            };

            bool wasPrinting = false;
            do
            {
                // Wait for the machine to be idle
                await machineIdle.WaitAsync(_disposedTCS.Token);

                // Try to get the next job
                Job job = null;
                if (wasPrinting)
                {
                    using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(_disposedTCS.Token);
                    cts.CancelAfter(1000);

                    try
                    {
                        job = await JobManager.Dequeue(cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        try
                        {
                            await SendCode("M98 P\"queue-end.g\"");
                            _logger.LogInformation("Print queue complete on {0}", Hostname);
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, "Failed to end print queue on {0}", Hostname);
                        }
                    }
                }
                else
                {
                    job = await JobManager.Dequeue(_disposedTCS.Token);
                }

                // Update this instance
                lock (this)
                {
                    Job = job;
                }

                // Try again if this was the last job
                if (job == null)
                {
                    wasPrinting = false;
                    continue;
                }

                // Got a new job
                lock (job)
                {
                    job.Hostname = Hostname;
                }
                _logger.LogInformation("Got {0} print job {1} for {2}", wasPrinting ? "next" : "new", job.Filename, Hostname);

                try
                {
                    // Upload the file
                    _logger.LogDebug("Uploading file {0} to {1}", job.Filename, Hostname);
                    using (FileStream fs = new(job.AbsoluteFilename, FileMode.Open, FileAccess.Read))
                    {
                        await _session.Upload($"0:/gcodes/{job.Filename}", fs, File.GetLastWriteTime(job.AbsoluteFilename));
                    }
                    _logger.LogDebug("Upload complete, running queue macro file and starting print");

                    // Run the corresponding macro file and start the next print file
                    await SendCode($"M98 P\"{(wasPrinting ? "queue-intermediate.g" : "queue-start.g")}\"");
                    await SendCode($"M32 \"{job.Filename}\"");
                    wasPrinting = true;

                    // Wait for the machine to start printing and for it to finish
                    await Task.Delay(2000, _disposedTCS.Token);
                    await machinePrinting.WaitAsync(_disposedTCS.Token);
                    await machineIdle.WaitAsync(_disposedTCS.Token);

                    // Print is complete
                    _logger.LogInformation("Finished print job {0} on {1}", job.Filename, Hostname);
                    lock (job)
                    {
                        job.Progress = null;
                        job.TimeLeft = null;
                        job.TimeCompleted = DateTime.Now;
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to start print job {0}, enqueuing it again", job.Filename);
                    lock (this)
                    {
                        Job = null;
                    }
                    await JobManager.Enqueue(job, _disposedTCS.Token);

                    // Wait a moment
                    await Task.Delay(2000);
                }
            }
            while (!disposed);
        }

        /// <summary>
        /// Send a code and throw an exception if it fails
        /// </summary>
        /// <param name="code">Code to send</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Asynchronous task</returns>
        /// <exception cref="Exception">Code generated an error</exception>
        private async Task SendCode(string code)
        {
            string reply = await _session.SendCode(code, _disposedTCS.Token);
            if (reply.StartsWith("Error:"))
            {
                _logger.LogError("[{0}] {1} => {2}", Hostname, code, reply.TrimEnd());
                throw new Exception(reply);
            }
            else if (reply.StartsWith("Warning:"))
            {
                _logger.LogWarning("[{0}] {1} => {2}", Hostname, code, reply.TrimEnd());
            }
            else if (!string.IsNullOrWhiteSpace(reply))
            {
                _logger.LogInformation("[{0}] {1} => {2}", Hostname, code, reply.TrimEnd());
            }
        }
    }
}
