using DuetAPI.ObjectModel;
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
    /// Interface for accessing the printer list
    /// </summary>
    public interface IPrinterList
    {
        /// <summary>
        /// Lock access to the printers
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
        /// Add a new printer
        /// </summary>
        /// <param name="hostname">Hostname to add</param>
        public void Add(string hostname);

        /// <summary>
        /// Suspend a printer
        /// </summary>
        /// <param name="hostname">Hostname of the printer</param>
        public void Suspend(string hostname);

        /// <summary>
        /// Resume the normal operation of a printer
        /// </summary>
        /// <param name="hostname">Hostname of the printer</param>
        public void Resume(string hostname);

        /// <summary>
        /// Delete an existing printer
        /// </summary>
        /// <param name="hostname">Hostname to delete</param>
        public void Remove(string hostname);

        /// <summary>
        /// Delegate for printer change events
        /// </summary>
        /// <param name="printer"></param>
        public delegate void PrinterChanged(Printer printer);

        /// <summary>
        /// Event to be called when a printer is added
        /// </summary>
        public event PrinterChanged OnPrinterAdded;

        /// <summary>
        /// Event to be called when a printer is suspended
        /// </summary>
        public event PrinterChanged OnPrinterSuspended;

        /// <summary>
        /// Event to be called when a printer is resumed (i.e. no longer paused)
        /// </summary>
        public event PrinterChanged OnPrinterResumed;

        /// <summary>
        /// Event to be called when a printer is removed
        /// </summary>
        public event PrinterChanged OnPrinterRemoved;
    }

    /// <summary>
    /// Singleton storing the list of printers
    /// </summary>
    public class PrinterList : IPrinterList
    {
        /// <summary>
        /// Logger instance
        /// </summary>
        private readonly ILogger<PrinterList> _logger;

        /// <summary>
        /// List of registered printer instances
        /// </summary>
        private List<Printer> _printers = new();

        /// <summary>
        /// Constructor of this class
        /// </summary>
        /// <param name="logger">Logger instance</param>
        public PrinterList(ILogger<PrinterList> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Lock for concurrent access to the printer list
        /// </summary>
        private readonly AsyncLock _lock = new();

        /// <summary>
        /// Lock access to the printers
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Disposable lock</returns>
        public AwaitableDisposable<IDisposable> LockAsync(CancellationToken cancellationToken = default) => _lock.LockAsync(cancellationToken);

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
                foreach (Printer printer in _printers)
                {
                    lock (printer)
                    {
                        JsonSerializer.Serialize(writer, printer);
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
            await JsonSerializer.SerializeAsync(fs, _printers, cancellationToken: cancellationToken);
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
                _printers = await JsonSerializer.DeserializeAsync<List<Printer>>(fs, cancellationToken: cancellationToken);
            }

            foreach (Printer printer in _printers)
            {
                printer.Status = MachineStatus.Disconnected;
                printer.Online = false;
                printer.JobFile = null;
                OnPrinterAdded?.Invoke(printer);
            }
        }

        /// <summary>
        /// Add a new printer
        /// </summary>
        /// <param name="hostname">Hostname to add</param>
        public void Add(string hostname)
        {
            foreach (Printer printer in _printers)
            {
                if (printer.Hostname == hostname)
                {
                    // Don't add the same printer twice
                    return;
                }
            }

            Printer newPrinter = new(hostname);
            _printers.Add(newPrinter);
            OnPrinterAdded?.Invoke(newPrinter);

            _logger.LogInformation("Printer {0} added", hostname);
        }

        /// <summary>
        /// Suspend a printer
        /// </summary>
        /// <param name="hostname">Hostname of the printer</param>
        public void Suspend(string hostname)
        {
            foreach (Printer printer in _printers)
            {
                if (printer.Hostname == hostname && !printer.Suspended)
                {
                    printer.Suspended = true;
                    OnPrinterSuspended?.Invoke(printer);
                    break;
                }
            }
        }

        /// <summary>
        /// Resume the normal operation of a printer
        /// </summary>
        /// <param name="hostname">Hostname of the printer</param>
        public void Resume(string hostname)
        {
            foreach (Printer printer in _printers)
            {
                if (printer.Hostname == hostname && printer.Suspended)
                {
                    printer.Suspended = false;
                    OnPrinterResumed?.Invoke(printer);
                    break;
                }
            }
        }

        /// <summary>
        /// Delete an existing printer
        /// </summary>
        /// <param name="hostname">Hostname to delete</param>
        public void Remove(string hostname)
        {
            foreach (Printer printer in _printers)
            {
                if (printer.Hostname == hostname)
                {
                    _printers.Remove(printer);
                    OnPrinterRemoved?.Invoke(printer);

                    _logger.LogInformation("Printer {0} removed", hostname);
                    break;
                }
            }
        }

        /// <summary>
        /// Event to be called when a printer is added
        /// </summary>
        public event IPrinterList.PrinterChanged OnPrinterAdded;

        /// <summary>
        /// Event to be called when a printer is suspended
        /// </summary>
        public event IPrinterList.PrinterChanged OnPrinterSuspended;

        /// <summary>
        /// Event to be called when a printer is resumed (i.e. no longer paused)
        /// </summary>
        public event IPrinterList.PrinterChanged OnPrinterResumed;

        /// <summary>
        /// Event to be called when a printer is removed
        /// </summary>
        public event IPrinterList.PrinterChanged OnPrinterRemoved;
    }
}
