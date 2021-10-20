using System;
using System.IO;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using DuetPrintFarm.Model;
using DuetPrintFarm.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DuetPrintFarm.Controllers
{
    /// <summary>
    /// MVC Controller for /printFarm requests
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class PrintFarmController : ControllerBase
    {
        /// <summary>
        /// App configuration
        /// </summary>
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Directory where G-code files are placed
        /// </summary>
        private string GCodesDirectory { get => _configuration.GetValue<string>("GCodesDirectory"); }

        /// <summary>
        /// Logger instance
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Create a new controller instance
        /// </summary>
        /// <param name="configuration">Launch configuration</param>
        /// <param name="logger">Logger instance</param>
        public PrintFarmController(IConfiguration configuration, ILogger<MachineController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// GET /printFarm/queue
        /// Retrieve the current print jobs
        /// </summary>
        /// <returns>
        /// HTTP status code:
        /// (200) Job Queue (JSON)
        /// </returns>
        [HttpGet("queue")]
        public async Task<IActionResult> Queue()
        {
            using (await JobManager.LockAsync())
            {
                return Content(JobManager.GetJson(), "application/json");
            }
        }

        /// <summary>
        /// GET /printFarm/printers
        /// Retrieve the configured printers
        /// </summary>
        /// <returns>
        /// HTTP status code:
        /// (200) Printer List (JSON)
        /// </returns>
        [HttpGet("printers")]
        public async Task<IActionResult> Printers()
        {
            using (await PrinterManager.LockAsync())
            {
                return Content(PrinterManager.GetJson(), "application/json");
            }
        }

        /// <summary>
        /// PUT /printFarm/printer?hostname={hostname}
        /// Add a new printer
        /// </summary>
        /// <returns>
        /// HTTP status code:
        /// (204) No Content
        /// (400) Invalid parameters
        /// (500) Generic error occurred
        /// </returns>
        [HttpPut("printer")]
        public async Task<IActionResult> AddPrinter(string hostname)
        {
            if (string.IsNullOrWhiteSpace(hostname))
            {
                return BadRequest();
            }

            try
            {
                await PrinterManager.AddPrinter(hostname);
                return NoContent();
            }
            catch (Exception e)
            {
                if (e is AggregateException ae)
                {
                    e = ae.InnerException;
                }
                _logger.LogError(e, $"[{nameof(AddPrinter)}] Failed add printer {hostname}");
                return StatusCode(500, e.Message);
            }
        }

        /// <summary>
        /// DELETE /printFarm/printer?hostname={hostname}
        /// Add a new printer
        /// </summary>
        /// <returns>
        /// HTTP status code:
        /// (204) No Content
        /// (400) Invalid parameters
        /// (500) Generic error occurred
        /// </returns>
        [HttpDelete("printer")]
        public async Task<IActionResult> DeletePrinter(string hostname)
        {
            if (string.IsNullOrWhiteSpace(hostname))
            {
                return BadRequest();
            }

            try
            {
                await PrinterManager.DeletePrinter(hostname);
                return NoContent();
            }
            catch (Exception e)
            {
                if (e is AggregateException ae)
                {
                    e = ae.InnerException;
                }
                _logger.LogError(e, $"[{nameof(DeletePrinter)}] Failed delete printer {hostname}");
                return StatusCode(500, e.Message);
            }
        }

        /// <summary>
        /// PUT /printFarm/job?filename={filename}
        /// Upload a file from the HTTP body and add it to the job queue
        /// </summary>
        /// <param name="filename">Destination of the file to upload</param>
        /// <returns>
        /// HTTP status code:
        /// (201) File created
        /// (400) Invalid parameters
        /// (500) Generic error occurred
        /// </returns>
        [DisableRequestSizeLimit]
        [HttpPut("job")]
        public async Task<IActionResult> AddFile(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
            {
                return BadRequest();
            }

            string resolvedPath = "n/a";
            try
            {
                resolvedPath = Path.Combine(GCodesDirectory, filename);

                // Write file
                using (FileStream stream = new(resolvedPath, FileMode.Create, FileAccess.Write))
                {
                    await Request.Body.CopyToAsync(stream);
                }

                // Enqueue it
                await JobManager.Enqueue(new Job() { AbsoluteFilename = resolvedPath });

                return Created(HttpUtility.UrlPathEncode(filename), null);
            }
            catch (Exception e)
            {
                if (e is AggregateException ae)
                {
                    e = ae.InnerException;
                }
                _logger.LogWarning(e, $"[{nameof(AddFile)} Failed upload file {filename} (resolved to {resolvedPath})");
                return StatusCode(500, e.Message);
            }
        }

        /// <summary>
        /// DELETE /printFarm/job?filename={filename}
        /// Remove a queued job file
        /// </summary>
        /// <param name="filename">Filename to remove</param>
        /// <returns>
        /// HTTP status code:
        /// (204) No Content
        /// (400) Invalid parameters
        /// (500) Generic error occurred
        /// </returns>
        [HttpDelete("job")]
        public async Task<IActionResult> RemoveFile(string filename, int? index)
        {
            if (string.IsNullOrWhiteSpace(filename) && index == null)
            {
                return BadRequest();
            }

            string resolvedPath = "n/a";
            try
            {
                if (filename != null)
                {
                    resolvedPath = Path.Combine(GCodesDirectory, filename);
                    await JobManager.Remove(filename);
                }
                else
                {
                    resolvedPath = $"Job #{index}";
                    await JobManager.Remove(index.Value);
                }

                return NoContent();
            }
            catch (Exception e)
            {
                if (e is AggregateException ae)
                {
                    e = ae.InnerException;
                }
                _logger.LogWarning(e, $"[{nameof(AddFile)} Failed remove file {filename ?? index.ToString()} (resolved to {resolvedPath})");
                return StatusCode(500, e.Message);
            }
        }
    }
}
