using DuetPrintFarm.Model;
using DuetPrintFarm.Singletons;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DuetPrintFarm.Services
{
    /// <summary>
    /// Service to manage the different printers
    /// </summary>
    public sealed class PrinterManager : IHostedService
    {
        /// <summary>
        /// Configuration of this application
        /// </summary>
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Logger instance
        /// </summary>
        private readonly ILogger<PrinterManager> _logger;

        /// <summary>
        /// Service provider instance
        /// </summary>
        private readonly IServiceProvider _provider;

        /// <summary>
        /// Job queue instance
        /// </summary>
        private readonly IJobQueue _jobQueue;

        /// <summary>
        /// List of printers
        /// </summary>
        private readonly IPrinterList _printerList;

        /// <summary>
        /// List of printer sessions
        /// </summary>
        private readonly List<PrinterSession> _printerSessions = new();

        /// <summary>
        /// File where the job queue is stored
        /// </summary>
        private string PrintersFile { get => _configuration.GetValue<string>("PrintersFile"); }

        /// <summary>
        /// Constructor of this service class
        /// </summary>
        /// <param name="configuration">App configuration</param>
        /// <param name="logger">Logger instance</param>
        /// <param name="printerList">List of configured printers</param>
        public PrinterManager(IConfiguration configuration, ILogger<PrinterManager> logger, IServiceProvider provider, IJobQueue jobQueue, IPrinterList printerList)
        {
            _configuration = configuration;
            _logger = logger;
            _provider = provider;
            _jobQueue = jobQueue;
            _printerList = printerList;
        }

        /// <summary>
        /// Start this service
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Asynchronous task</returns>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _jobQueue.OnPauseJob += PauseJob;
            _jobQueue.OnResumeJob += ResumeJob;
            _jobQueue.OnCancelJob += CancelJob;
            _printerList.OnPrinterAdded += PrinterAdded;
            _printerList.OnPrinterSuspended += PrinterSuspended;
            _printerList.OnPrinterResumed += PrinterResumed;
            _printerList.OnPrinterRemoved += PrinterRemoved;

            // Load the configured printers if possible
            if (File.Exists(PrintersFile))
            {
                using (await _printerList.LockAsync(cancellationToken))
                {
                    await _printerList.LoadFromFileAsync(PrintersFile, cancellationToken);
                }
                _logger.LogInformation("Printers loaded from {0}", PrintersFile);
            }
        }

        /// <summary>
        /// Pause a job item
        /// </summary>
        /// <param name="job">Job to pause</param>
        private async void PauseJob(Job job)
        {
            foreach (PrinterSession session in _printerSessions)
            {
                if (session.Printer.Hostname == job.Hostname)
                {
                    if (await session.PauseAsync())
                    {
                        lock (job)
                        {
                            job.Paused = true;
                        }
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Resume a job item
        /// </summary>
        /// <param name="job">Job to resume</param>
        private async void ResumeJob(Job job)
        {
            foreach (PrinterSession session in _printerSessions)
            {
                if (session.Printer.Hostname == job.Hostname)
                {
                    if (await session.ResumeAsync())
                    {
                        lock (job)
                        {
                            job.Paused = false;
                        }
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Cancel a job item
        /// </summary>
        /// <param name="job">Job to cancel</param>
        private async void CancelJob(Job job)
        {
            foreach (PrinterSession session in _printerSessions)
            {
                if (session.Printer.Hostname == job.Hostname)
                {
                    if (await session.CancelAsync())
                    {
                        lock (job)
                        {
                            job.Cancelled = true;
                            job.Paused = false;
                        }
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Called when a printer has been added
        /// </summary>
        /// <param name="printer">New printer</param>
        private void PrinterAdded(Printer printer)
        {
            PrinterSession session = ActivatorUtilities.CreateInstance<PrinterSession>(_provider, printer);
            _printerSessions.Add(session);
        }

        /// <summary>
        /// Called when a printer has been suspended
        /// </summary>
        /// <param name="printer">Resumed printer</param>
        private void PrinterSuspended(Printer printer)
        {
            foreach (PrinterSession session in _printerSessions)
            {
                if (session.Printer == printer)
                {
                    session.Suspend();
                    break;
                }
            }
        }

        /// <summary>
        /// Called when a printer has been resumed (i.e. it is no longer paused)
        /// </summary>
        /// <param name="printer">Resumed printer</param>
        private void PrinterResumed(Printer printer)
        {
            foreach (PrinterSession session in _printerSessions)
            {
                if (session.Printer == printer)
                {
                    session.Resume();
                    break;
                }
            }
        }

        /// <summary>
        /// Called when a printer has been removed
        /// </summary>
        /// <param name="printer">Deleted printer</param>
        private async void PrinterRemoved(Printer printer)
        {
            foreach (PrinterSession session in _printerSessions)
            {
                if (session.Printer == printer)
                {
                    _printerSessions.Remove(session);
                    await session.DisposeAsync();
                    break;
                }
            }
        }

        /// <summary>
        /// Stop this service
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Asynchronous task</returns>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _jobQueue.OnPauseJob -= PauseJob;
            _jobQueue.OnResumeJob -= ResumeJob;
            _jobQueue.OnCancelJob -= CancelJob;
            _printerList.OnPrinterAdded -= PrinterAdded;
            _printerList.OnPrinterSuspended -= PrinterSuspended;
            _printerList.OnPrinterResumed -= PrinterResumed;
            _printerList.OnPrinterRemoved -= PrinterRemoved;

            // Save the list of printers again
            using (await _printerList.LockAsync(cancellationToken))
            {
                await _printerList.SaveToFileAsync(PrintersFile, cancellationToken);
                _logger.LogInformation("Printers saved to {0}", PrintersFile);
            }

            // Disconnect all the sessions again
            List<Task> sessionTasks = new();
            foreach (PrinterSession session in _printerSessions)
            {
                sessionTasks.Add(session.Task);
                await session.DisposeAsync();
            }

            // Wait for every session to terminate
            try
            {
                await Task.WhenAll(sessionTasks);
            }
            catch (OperationCanceledException)
            {
                // can be expected
            }

            // Log this
            _logger.LogInformation("Printers disconnected");
        }
    }
}
