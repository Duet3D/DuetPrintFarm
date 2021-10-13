using System;
using System.IO;
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
        /// <returns>Current job queue as JSON</returns>
        [HttpGet]
        public async Task<IActionResult> Queue()
        {
            using (await JobManager.LockAsync(default))
            {
                string json = JsonSerializer.Serialize(JobManager.Jobs);
                return Content(json);
            }
        }

        /// <summary>
        /// GET /printFarm/printers
        /// Retrieve the configured printers
        /// </summary>
        /// <returns>Asynchronous task</returns>
        [HttpGet]
        public async Task<IActionResult> Printers()
        {
            using (await PrinterManager.LockAsync(default))
            {
                string json = JsonSerializer.Serialize(PrinterManager.Printers);
                return Content(json);
            }
        }

        /// <summary>
        /// PUT /printFarm/printer
        /// Add a new printer
        /// </summary>
        /// <returns></returns>
        [HttpPut("printer")]
        public async Task<IActionResult> AddPrinter()
        {
            string hostname;
            using (StreamReader reader = new(Request.Body, Encoding.UTF8))
            {
                hostname = await reader.ReadToEndAsync();
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
        /// DELETE /printFarm/printer
        /// Add a new printer
        /// </summary>
        /// <returns></returns>
        [HttpDelete("printer")]
        public async Task<IActionResult> DeletePrinter()
        {
            string hostname;
            using (StreamReader reader = new(Request.Body, Encoding.UTF8))
            {
                hostname = await reader.ReadToEndAsync();
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
        /// PUT /printFarm/job/{filename}
        /// Upload a file from the HTTP body and add it to the job queue
        /// </summary>
        /// <param name="filename">Destination of the file to upload</param>
        /// <returns>
        /// HTTP status code:
        /// (201) File created
        /// (500) Generic error occurred
        /// </returns>
        [DisableRequestSizeLimit]
        [HttpPut("job/{*filename}")]
        public async Task<IActionResult> EnqueueFile(string filename)
        {
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
                await JobManager.Enqueue(new Job() { Filename = resolvedPath });

                return Created(HttpUtility.UrlPathEncode(filename), null);
            }
            catch (Exception e)
            {
                if (e is AggregateException ae)
                {
                    e = ae.InnerException;
                }
                _logger.LogWarning(e, $"[{nameof(EnqueueFile)} Failed upload file {filename} (resolved to {resolvedPath})");
                return StatusCode(500, e.Message);
            }
        }
    }
}
