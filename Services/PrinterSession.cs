using DuetAPI.ObjectModel;
using DuetHttpClient;
using DuetPrintFarm.Model;
using DuetPrintFarm.Singletons;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Job = DuetPrintFarm.Model.Job;

namespace DuetPrintFarm.Services
{
    public class PrinterSession : IAsyncDisposable
    {
        /// <summary>
        /// Printer that is maintained by this session
        /// </summary>
        public Printer Printer { get; }

        /// <summary>
        /// Logger instance
        /// </summary>
        private readonly ILogger<PrinterSession> _logger;

        /// <summary>
        /// Job queue instance
        /// </summary>
        private readonly IJobQueue _jobQueue;

        /// <summary>
        /// Task that is used to maintain this printer
        /// </summary>
        public Task Task { get; }

        /// <summary>
        /// Constructor of this class
        /// </summary>
        /// <param name="printer">Printer instance of this session</param>
        /// <param name="logger">Logger instance</param>
        /// <param name="jobQueue">Global job queue</param>
        public PrinterSession(Printer printer, ILogger<PrinterSession> logger, IJobQueue jobQueue)
        {
            Printer = printer;
            _logger = logger;
            _jobQueue = jobQueue;

            if (!printer.Suspended)
            {
                _machineActive.Set();
            }
            Task = Task.Run(MaintainPrinter);
        }

        /// <summary>
        /// Remote HTTP session of this printer 
        /// </summary>
        private DuetHttpSession _httpSession;

        /// <summary>
        /// CTS to be triggered when this instance is _disposed
        /// </summary>
        private readonly CancellationTokenSource _disposedCts = new();

        /// <summary>
        /// Whether this instance is _disposed
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Dispose this instance asynchronously
        /// </summary>
        /// <returns>Asynchronous task</returns>
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;

            _machineDisconnectedCts.Cancel();
            _disposedCts.Cancel();
            if (_httpSession != null)
            {
                await _httpSession.DisposeAsync();
            }

            _machineDisconnectedCts.Dispose();
            _disposedCts.Dispose();
        }

        /// <summary>
        /// Current job of this printer session
        /// </summary>
        private Job _job;

        /// <summary>
        /// Manual reset event to be triggered when the machine is ready for printing
        /// </summary>
        private readonly AsyncManualResetEvent _machineActive = new();

        /// <summary>
        /// Manual reset event to be triggered when the machine is idle
        /// </summary>
        private readonly AsyncManualResetEvent _machineIdle = new();

        /// <summary>
        /// Manual reset event to be triggered when the machine is printing
        /// </summary>
        private readonly AsyncManualResetEvent _machinePrinting = new();

        /// <summary>
        /// Manual reset event to be triggered when the machine has disconnected
        /// </summary>
        private CancellationTokenSource  _machineDisconnectedCts = new();

        /// <summary>
        /// Method to keep the session 
        /// </summary>
        /// <returns>Asynchronous task</returns>
        private async Task MaintainPrinter()
        {
            // Establish a new session first
            do
            {
                try
                {
                    _httpSession = await DuetHttpSession.ConnectAsync(new Uri($"http://{Printer.Hostname}"), cancellationToken: _disposedCts.Token);
                    await _httpSession.WaitForModelUpdate(_disposedCts.Token);
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    _logger.LogWarning("[{0}] Failed to connect to printer: {1}", Printer.Name, e.Message);
                    await Task.Delay(2000, _disposedCts.Token);
                }
            }
            while (_httpSession == null);

            // Keep the estimated times left and file progress up-to-date
            lock (_httpSession.Model)
            {
                lock (Printer)
                {
                    Printer.Name = _httpSession.Model.Network.Name;
                    Printer.Status = _httpSession.Model.State.Status;

                    if (_httpSession.Model.State.Status == MachineStatus.Idle)
                    {
                        _machineIdle.Set();
                    }
                    else if (_httpSession.Model.State.Status == MachineStatus.Processing)
                    {
                        _machinePrinting.Set();
                    }

                    if (_httpSession.Model.State.Status != MachineStatus.Starting &&
                        _httpSession.Model.State.Status != MachineStatus.Halted &&
                        _httpSession.Model.State.Status != MachineStatus.Updating &&
                        _httpSession.Model.State.Status != MachineStatus.Disconnected)
                    {
                        Printer.Online = true;
                        _logger.LogInformation("[{0}] Printer is now online", Printer.Name);
                    }

                    if (!string.IsNullOrEmpty(_httpSession.Model.Job.File.FileName))
                    {
                        Printer.JobFile = Path.GetFileName(_httpSession.Model.Job.File.FileName);
                        _logger.LogInformation("[{0}] Printer is printing {1}", Printer.Name, Printer.JobFile);
                    }
                }

                _httpSession.Model.Job.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == nameof(ObjectModel.Job.Duration))
                    {
                        long? printTime = _httpSession.Model.Job.File.PrintTime ?? _httpSession.Model.Job.File.SimulatedTime;
                        if (printTime != null)
                        {
                            lock (this)
                            {
                                if (_job != null)
                                {
                                    lock (_job)
                                    {
                                        if (_job.TimeCompleted == null)
                                        {
                                            if (_httpSession.Model.Job.Duration != null)
                                            {
                                                _job.TimeLeft = Math.Max(printTime.Value - _httpSession.Model.Job.Duration.Value, 0);
                                            }
                                            else
                                            {
                                                _job.TimeLeft = null;
                                            }
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
                            if (_job != null)
                            {
                                lock (_job)
                                {
                                    if (_job.TimeCompleted == null)
                                    {
                                        if (_httpSession.Model.Job.FilePosition != null && _httpSession.Model.Job.File.Size > 0)
                                        {
                                            _job.Progress = (double)_httpSession.Model.Job.FilePosition / _httpSession.Model.Job.File.Size;
                                        }
                                        else if (_httpSession.Model.State.Status == MachineStatus.Idle)
                                        {
                                            _job.Progress = null;
                                        }
                                    }
                                }
                            }
                        }
                    }
                };

                // Watch for job file changes
                _httpSession.Model.Job.File.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == nameof(ObjectModel.Job.File.FileName))
                    {
                        if (string.IsNullOrEmpty(_httpSession.Model.Job.File.FileName))
                        {
                            // No longer printing
                            lock (Printer)
                            {
                                Printer.JobFile = null;
                            }
                            _logger.LogInformation("[{0}] Printer is no longer printing", Printer.Name);
                        }
                        else
                        {
                            // Now printing
                            string jobFile;
                            lock (_httpSession.Model)
                            {
                                jobFile = Path.GetFileName(_httpSession.Model.Job.File.FileName);
                            }
                            lock (Printer)
                            {
                                Printer.JobFile = jobFile;
                            }
                            _logger.LogInformation("[{0}] Printer is printing {1}", Printer.Name, jobFile);
                        }
                    }
                };

                // Watch for name changes
                _httpSession.Model.Network.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == nameof(ObjectModel.Network.Name))
                    {
                        _logger.LogInformation("[{0}] Printer has changed its name to {1}", Printer.Name, _httpSession.Model.Network.Name);
                        lock (Printer)
                        {
                            Printer.Name = _httpSession.Model.Network.Name;
                        }
                    }
                };

                // Watch for state changes
                _httpSession.Model.State.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == nameof(ObjectModel.State.Status))
                    {
                        bool isOnline = (_httpSession.Model.State.Status != MachineStatus.Starting) &&
                                        (_httpSession.Model.State.Status != MachineStatus.Halted) &&
                                        (_httpSession.Model.State.Status != MachineStatus.Updating) &&
                                        (_httpSession.Model.State.Status != MachineStatus.Disconnected);
                        lock (Printer)
                        {
                            Printer.Status = _httpSession.Model.State.Status;
                            if (Printer.Online != isOnline)
                            {
                                Printer.Online = isOnline;
                                _logger.LogInformation("[{0}] Printer is now {1}", Printer.Name, isOnline ? "online" : "offline");

                                if (!isOnline)
                                {
                                    Printer.JobFile = null;
                                    if (_job != null)
                                    {
                                        _machineDisconnectedCts.Cancel();
                                    }
                                }
                            }
                        }

                        if (_httpSession.Model.State.Status == MachineStatus.Idle)
                        {
                            _machinePrinting.Reset();
                            _machineIdle.Set();
                        }
                        else if (_httpSession.Model.State.Status == MachineStatus.Processing)
                        {
                            _machineIdle.Reset();
                            _machinePrinting.Set();
                        }
                        else
                        {
                            _machineIdle.Reset();
                            _machinePrinting.Reset();
                        }
                    }
                };
            }

            // Check if there is a leftover print job due to a restart of this application
            await _jobQueue.WaitToBeReady(_disposedCts.Token);

            Job job, nextJob = null;
            using (await _jobQueue.LockAsync(_disposedCts.Token))
            {
                job = _jobQueue.FindJob(Printer.Hostname);
            }

            // Start processing the global print queue
            bool wasPrinting = job != null, jobResumed = wasPrinting;
            do
            {
                // Wait for the machine to be active and ready
                await _machineActive.WaitAsync(_disposedCts.Token);
                if (!jobResumed)
                {
                    await _machineIdle.WaitAsync(_disposedCts.Token);

                    // Try to get the next job
                    if (job == null)
                    {
                        using (await _jobQueue.LockAsync())
                        {
                            job = await _jobQueue.DequeueAsync(_disposedCts.Token);
                            lock (job)
                            {
                                job.Hostname = Printer.Hostname;
                            }
                        }
                    }
                }

                // Make sure the job file exists
                if (!File.Exists(job.AbsoluteFilename))
                {
                    _logger.LogError("[{0}] Job file {1} does not exist, marking it as cancelled", Printer.Name, job.Filename);
                    lock (job)
                    {
                        job.Cancelled = true;
                    }
                    using (await _jobQueue.LockAsync(_disposedCts.Token))
                    {
                        _jobQueue.PrintFinished(job);
                    }

                    job = null;
                    continue;
                }

                // Make sure this printer wasn't suspended while waiting for a new job
                bool isSuspended;
                lock (Printer)
                {
                    isSuspended = Printer.Suspended;
                }

                if (isSuspended)
                {
                    _logger.LogDebug("[{0}] Attempted to get print job {1} even though the printer is suspended", Printer.Name, job.Filename);
                    using (await _jobQueue.LockAsync(_disposedCts.Token))
                    {
                        _jobQueue.Enqueue(job);
                    }

                    job = null;
                    continue;
                }

                // Got a new job and the machine is ready
                lock (this)
                {
                    _job = job;
                }
                lock (Printer)
                {
                    Printer.JobFile = job.Filename;
                }
                _logger.LogInformation("[{0}] Got {1} print job {2}", Printer.Name, wasPrinting ? "next" : "new", job.Filename);

                try
                {
                    if (!jobResumed)
                    {
                        // Upload the file
                        _logger.LogDebug("[{0}] Uploading file {0}", Printer.Name, job.Filename);
                        await using (FileStream fs = new(job.AbsoluteFilename, FileMode.Open, FileAccess.Read))
                        {
                            await _httpSession.Upload($"0:/gcodes/{job.Filename}", fs, File.GetLastWriteTime(job.AbsoluteFilename));
                        }

                        _logger.LogDebug("[{0}] Upload complete, running queue macro file and starting print", Printer.Name);

                        // Run the corresponding macro file
                        lock (job)
                        {
                            job.ProgressText = wasPrinting ? "starting next" : "starting";
                        }

                        await SendCode($"M98 P\"{(wasPrinting ? "queue-intermediate.g" : "queue-start.g")}\"");
                        await WaitForIdle();

                        // Start the actual print file
                        await SendCode($"M32 \"{job.Filename}\"");
                        wasPrinting = true;
                    }

                    // Wait for the machine to start printing and for it to finish
                    await WaitForPrintStart();
                    lock (job)
                    {
                        job.ProgressText = null;
                    }
                    await WaitForIdle();

                    // Try to get the next job
                    nextJob = null;
                    lock (Printer)
                    {
                        isSuspended = Printer.Suspended;
                    }
                    if (!isSuspended)
                    {
                        using (await _jobQueue.LockAsync(_disposedCts.Token))
                        {
                            if (_jobQueue.TryDequeue(out nextJob))
                            {
                                lock (nextJob)
                                {
                                    nextJob.Hostname = Printer.Hostname;
                                }
                            }
                        }
                    }

                    // Is this the last one?
                    if (nextJob == null)
                    {
                        lock (job)
                        {
                            job.ProgressText = "finishing";
                        }

                        try
                        {
                            await SendCode("M98 P\"queue-end.g\"");
                            _logger.LogInformation("[{0}] Print queue complete", Printer.Name);
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, "[{0}] Failed to end print queue", Printer.Name);
                        }

                        wasPrinting = false;
                    }

                    // Remove the print job again from the remote machine
                    try
                    {
                        await _httpSession.Delete($"0:/gcodes/{job.Filename}");
                    }
                    catch (Exception e)
                    {
                        _logger.LogWarning(e, "[{0}] Could not delete finished job {1}", Printer.Name, job.Filename);
                    }

                    // Print complete
                    _logger.LogInformation("[{0}] Finished{1} print job {2}", Printer.Name, wasPrinting ? string.Empty : " last", job.Filename);
                    lock (this)
                    {
                        _job = null;
                    }
                    using (await _jobQueue.LockAsync(_disposedCts.Token))
                    {
                        _jobQueue.PrintFinished(job);
                    }

                    // Move on to the next file (if applicable)
                    job = nextJob;
                    nextJob = null;
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    // Log a message
                    if (e is IOException)
                    {
                        _logger.LogError("[{0}] Printer has gone offline unexpectedly, enqueing job {1} again", Printer.Name, job.Filename);
                    }
                    else if (!_disposed)
                    {
                        _logger.LogError(e, "[{0}] Failed to print job {1}, enqueuing it again", Printer.Name, job.Filename);
                    }

                    // Enqueue the job again
                    lock (this)
                    {
                        _job = null;
                    }
                    using (await _jobQueue.LockAsync(_disposedCts.Token))
                    {
                        _jobQueue.Enqueue(nextJob ?? job);
                    }
                    job = nextJob = null;
                    wasPrinting = false;

                    // Wait a moment
                    await Task.Delay(2000, _disposedCts.Token);

                    // Renew the cancellation token
                    _machineDisconnectedCts.Dispose();
                    _machineDisconnectedCts = new();
                }

                jobResumed = false;
            }
            while (!_disposed);
        }

        /// <summary>
        /// Wait for the machine to be idle again. If the machine disconnects unexpectedly, an exception is thrown
        /// </summary>
        /// <returns></returns>
        /// <exception cref="IOException">Machine has gone offline</exception>
        private async Task WaitForIdle()
        {
            try
            {
                await _machineIdle.WaitAsync(_machineDisconnectedCts.Token);
            }
            catch (OperationCanceledException) when (!_disposed)
            {
                throw new IOException("Printer has gone offline");
            }
        }

        /// <summary>
        /// Wait for the machine to start printing. If the machine disconnects unexpectedly, an exception is thrown
        /// </summary>
        /// <returns></returns>
        /// <exception cref="IOException">Machine has gone offline</exception>
        private async Task WaitForPrintStart()
        {
            try
            {
                await _machinePrinting.WaitAsync(_machineDisconnectedCts.Token);
            }
            catch (OperationCanceledException)
            {
                if (_machineDisconnectedCts.IsCancellationRequested)
                {
                    throw new IOException("Printer has gone offline");
                }
            }
        }

        /// <summary>
        /// Called to suspend this printer session
        /// </summary>
        public void Suspend() => _machineActive.Reset();

        /// <summary>
        /// Called to resume the normal operation of a printer
        /// </summary>
        public void Resume() => _machineActive.Set();

        /// <summary>
        /// Send a code and throw an exception if it fails
        /// </summary>
        /// <param name="code">Code to send</param>
        /// <returns>Asynchronous task</returns>
        /// <exception cref="Exception">Code generated an error</exception>
        private async Task SendCode(string code)
        {
            string reply = await _httpSession.SendCode(code, _disposedCts.Token);
            if (reply.StartsWith("Error:"))
            {
                _logger.LogError("[{0}] {1} => {2}", Printer.Name, code, reply.TrimEnd());
                throw new Exception(reply);
            }
            else if (reply.StartsWith("Warning:"))
            {
                _logger.LogWarning("[{0}] {1} => {2}", Printer.Name, code, reply.TrimEnd());
            }
            else if (!string.IsNullOrWhiteSpace(reply))
            {
                _logger.LogInformation("[{0}] {1} => {2}", Printer.Name, code, reply.TrimEnd());
            }
        }

        /// <summary>
        /// Pause the machine
        /// </summary>
        /// <returns>True on success</returns>
        public async Task<bool> PauseAsync()
        {
            lock (_httpSession.Model)
            {
                if (_httpSession.Model.State.Status is MachineStatus.Pausing or MachineStatus.Paused)
                {
                    // Already paused
                    return true;
                }
            }

            try
            {
                await SendCode("M25");
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Resume the machine
        /// </summary>
        /// <returns>True on success</returns>
        public async Task<bool> ResumeAsync()
        {
            lock (_httpSession.Model)
            {
                if (_httpSession.Model.State.Status is MachineStatus.Resuming or MachineStatus.Processing)
                {
                    // Already resumed
                    return true;
                }
            }

            try
            {
                await SendCode("M24");
                return true;
            }
            catch
            {
                return false;
            }
        }


        /// <summary>
        /// Cancel the current print job
        /// </summary>
        /// <returns>True on success</returns>
        public async Task<bool> CancelAsync()
        {
            lock (_httpSession.Model)
            {
                if (_httpSession.Model.State.Status is MachineStatus.Cancelling or MachineStatus.Idle)
                {
                    // Already cancelled
                    return true;
                }
            }

            try
            {
                await SendCode("M0");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
