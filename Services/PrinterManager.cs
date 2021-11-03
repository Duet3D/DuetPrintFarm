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
        public PrinterManager(IConfiguration configuration, ILogger<PrinterManager> logger, IServiceProvider provider, IPrinterList printerList)
        {
            _configuration = configuration;
            _logger = logger;
            _provider = provider;
            _printerList = printerList;
        }

        /// <summary>
        /// Start this service
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Asynchronous task</returns>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
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
