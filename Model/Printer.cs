using DuetAPI.ObjectModel;
using DuetHttpClient;
using DuetPrintFarm.Services;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using System;
using System.IO;
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
        public Task SessionTask { get; }

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
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Failed to connect to printer {0}", Hostname);
                    await Task.Delay(2000);
                }

                if (disposed)
                {
                    // Stop if the printer has been deleted
                    return;
                }
            }
            while (_session == null);

            // Watch for job file changes
            _session.Model.Job.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(ObjectModel.Job.File))
                {
                    if (_session.Model.Job.File == null)
                    {
                        // No longer printing
                        lock (this)
                        {
                            Job = null;
                            JobFile = null;
                            _logger.LogInformation("Printer {0} is no longer printing", Hostname);
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
                    }
                }
            };

            // Watch for state changes
            AsyncManualResetEvent machineIdle = new();
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

                    if (_session.Model.State.Status == MachineStatus.Idle)
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
                Job nextJob = null;
                if (wasPrinting)
                {
                    using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(_disposedTCS.Token);
                    cts.CancelAfter(1000);

                    try
                    {
                        nextJob = await JobManager.Dequeue(cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        try
                        {
                            await _session.SendCode("M98 P\"queue-end.g\"");
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
                    nextJob = await JobManager.Dequeue(_disposedTCS.Token);
                }

                // Update this instance
                lock (this)
                {
                    Job = nextJob;
                }

                // Try again if this was the last job
                if (nextJob == null)
                {
                    wasPrinting = false;
                    continue;
                }
                _logger.LogInformation("Got {0} print job {1} for {2}", wasPrinting ? "next" : "new", nextJob.ShortName, Hostname);

                try
                {
                    // Upload the file
                    _logger.LogDebug("Uploading file {0} to {1}", nextJob.ShortName, Hostname);
                    using (FileStream fs = new(nextJob.Filename, FileMode.Open, FileAccess.Read))
                    {
                        await _session.Upload(nextJob.ShortName, fs, File.GetLastWriteTime(nextJob.Filename));
                    }
                    _logger.LogDebug("Upload complete, running queue macro file and starting print");

                    // Run the corresponding macro file and start the next print file
                    await SendCode($"M98 P\"{(wasPrinting ? "queue-intermediate.g" : "queue-start.g")}\"");
                    await SendCode($"M32 \"{nextJob.ShortName}\"");
                    wasPrinting = true;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to start print job {0}, enqueuing it again", nextJob.ShortName);
                    lock (this)
                    {
                        Job = null;
                    }
                    await JobManager.Enqueue(nextJob, _disposedTCS.Token);

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
        private async Task SendCode(string code)
        {
            string reply = await _session.SendCode(code, _disposedTCS.Token);
            if (reply.StartsWith("Error:"))
            {
                _logger.LogError("{0} => {1}", code, reply);
                throw new Exception(reply);
            }
            else if (reply.StartsWith("Warning:"))
            {
                _logger.LogWarning("{0} => {1}", code, reply);
            }
            else if (!string.IsNullOrWhiteSpace(reply))
            {
                _logger.LogInformation("{0} => {1}", code, reply);
            }
        }
    }
}
