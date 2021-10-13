using DuetPrintFarm.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
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
        /// File where the job queue is stored
        /// </summary>
        private string PrintersFile { get => _configuration.GetValue<string>("PrintersFile"); }

        /// <summary>
        /// Logger instance
        /// </summary>
        private static ILogger<PrinterManager> _logger;

        /// <summary>
        /// Lock for concurrent access to the printer list
        /// </summary>
        private static readonly AsyncLock _lock = new();

        /// <summary>
        /// Lock access to the printers
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Disposable lock</returns>
        public static AwaitableDisposable<IDisposable> LockAsync(CancellationToken cancellationToken) => _lock.LockAsync(cancellationToken);

        /// <summary>
        /// List of registered printer instances
        /// </summary>
        public static List<Printer> Printers { get; } = new();

        /// <summary>
        /// Add a new printer
        /// </summary>
        /// <param name="hostname">Hostname to add</param>
        /// <returns>Asynchronous task</returns>
        public static async Task AddPrinter(string hostname, CancellationToken cancellationToken = default)
        {
            using (await _lock.LockAsync(cancellationToken))
            {
                foreach (Printer printer in Printers)
                {
                    if (printer.Hostname == hostname)
                    {
                        // Don't add the same printer twice
                        return;
                    }
                }

                Printers.Add(new Printer(hostname, _logger));
                _logger.LogInformation("Printer {0} added", hostname);
            }
        }

        /// <summary>
        /// Delete an existing printer
        /// </summary>
        /// <param name="hostname">Hostname to delete</param>
        /// <returns>Asynchronous task</returns>
        public static async Task DeletePrinter(string hostname, CancellationToken cancellationToken = default)
        {
            using (await _lock.LockAsync(cancellationToken))
            {
                foreach (Printer printer in Printers)
                {
                    if (printer.Hostname == hostname)
                    {
                        if (printer.Job != null)
                        {
                            // Enqueue the printer's current job again so it doesn't get lost
                            await JobManager.Enqueue(printer.Job, cancellationToken);
                        }

                        Printers.Remove(printer);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Constructor of this service class
        /// </summary>
        /// <param name="configuration">App configuration</param>
        /// <param name="logger">Logger instance</param>
        public PrinterManager(IConfiguration configuration, ILogger<PrinterManager> logger)
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
            // Load the configured printers if possible
            if (File.Exists(PrintersFile))
            {
                using FileStream fs = new(PrintersFile, FileMode.Open, FileAccess.Read);
                using StreamReader reader = new(fs);
                using (await _lock.LockAsync(cancellationToken))
                {
                    while (!reader.EndOfStream)
                    {
                        string hostname = await reader.ReadLineAsync();
                        Printers.Add(new Printer(hostname, _logger));
                    }
                }
            }

            // Log this
            _logger.LogInformation("Printer manager started");
        }

        /// <summary>
        /// Stop this service
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Asynchronous task</returns>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            // Save the list of printers again
            using FileStream fs = new(PrintersFile, FileMode.Create, FileAccess.Write);
            using StreamWriter writer = new(fs);
            using (await _lock.LockAsync(cancellationToken))
            {
                foreach (Printer printer in Printers)
                {
                    await writer.WriteLineAsync(printer.Hostname);
                }
            }

            // Disconnect all the sessions again
            List<Task> sessionTasks = new();
            foreach (Printer printer in Printers)
            {
                sessionTasks.Add(printer.SessionTask);
                await printer.DisposeAsync();
            }
            await Task.WhenAll(sessionTasks);

            // Log this
            _logger.LogInformation("Printer manager stopped");
        }
    }
}
