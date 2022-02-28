using System;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using DuetPrintFarm.Model;
using DuetPrintFarm.Singletons;
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
        /// Job queue instance
        /// </summary>
        private readonly IJobQueue _jobQueue;

        /// <summary>
        /// Printer list
        /// </summary>
        private readonly IPrinterList _printerList;

        /// <summary>
        /// Create a new controller instance
        /// </summary>
        /// <param name="configuration">Launch configuration</param>
        /// <param name="logger">Logger instance</param>
        public PrintFarmController(IConfiguration configuration, ILogger<MachineController> logger, IJobQueue jobQueue, IPrinterList printerList)
        {
            _configuration = configuration;
            _logger = logger;
            _jobQueue = jobQueue;
            _printerList = printerList;
        }

        #region Job Queue Requests
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
            using (await _jobQueue.LockAsync())
            {
                return Content(_jobQueue.ToJson(), "application/json");
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
                await using (FileStream stream = new(resolvedPath, FileMode.Create, FileAccess.Write))
                {
                    await Request.Body.CopyToAsync(stream);
                }

                // Enqueue it
                using (await _jobQueue.LockAsync())
                {
                    _jobQueue.Enqueue(new Job() { AbsoluteFilename = resolvedPath });
                }

                return Created(HttpUtility.UrlPathEncode(filename), null);
            }
            catch (Exception e)
            {
                if (e is AggregateException ae)
                {
                    e = ae.InnerException;
                }
                _logger.LogWarning(e, $"[{nameof(AddFile)}] Failed upload file {filename} (resolved to {resolvedPath})");
                return StatusCode(500, e.Message);
            }
        }

        /// <summary>
        /// POST /printFarm/pause?index={index}
        /// Pause a job file from the queue
        /// </summary>
        /// <param name="index">Job index to pause</param>
        /// <returns>
        /// HTTP status code:
        /// (204) No Content
        /// (400) Invalid parameters
        /// (500) Generic error occurred
        /// </returns>
        [HttpPost("pause")]
        public async Task<IActionResult> PauseFile(int index)
        {
            try
            {
                using (await _jobQueue.LockAsync())
                {
                    if (_jobQueue.Pause(index))
                    {
                        return NoContent();
                    }
                    return BadRequest();
                }
            }
            catch (Exception e)
            {
                if (e is AggregateException ae)
                {
                    e = ae.InnerException;
                }
                _logger.LogWarning(e, $"[{nameof(RepeatFile)}] Failed repeat file #{index}");
                return StatusCode(500, e.Message);
            }
        }

        /// <summary>
        /// POST /printFarm/resume?index={index}
        /// Resume a job file from the queue
        /// </summary>
        /// <param name="index">Job index to resume</param>
        /// <returns>
        /// HTTP status code:
        /// (204) No Content
        /// (400) Invalid parameters
        /// (500) Generic error occurred
        /// </returns>
        [HttpPost("resume")]
        public async Task<IActionResult> ResumeFile(int index)
        {
            try
            {
                using (await _jobQueue.LockAsync())
                {
                    if (_jobQueue.Resume(index))
                    {
                        return NoContent();
                    }
                    return BadRequest();
                }
            }
            catch (Exception e)
            {
                if (e is AggregateException ae)
                {
                    e = ae.InnerException;
                }
                _logger.LogWarning(e, $"[{nameof(ResumeFile)}] Failed resume file #{index}");
                return StatusCode(500, e.Message);
            }
        }

        /// <summary>
        /// POST /printFarm/cancel?index={index}
        /// Cancel a job file from the queue
        /// </summary>
        /// <param name="filename">Filename to cancel</param>
        /// <param name="filename">Job index to cancel</param>
        /// <returns>
        /// HTTP status code:
        /// (204) No Content
        /// (400) Invalid parameters
        /// (500) Generic error occurred
        /// </returns>
        [HttpPost("cancel")]
        public async Task<IActionResult> CancelFile(int index)
        {
            try
            {
                using (await _jobQueue.LockAsync())
                {
                    if (_jobQueue.Cancel(index))
                    {
                        return NoContent();
                    }
                    return BadRequest();
                }
            }
            catch (Exception e)
            {
                if (e is AggregateException ae)
                {
                    e = ae.InnerException;
                }
                _logger.LogWarning(e, $"[{nameof(CancelFile)}] Failed resume file #{index}");
                return StatusCode(500, e.Message);
            }
        }

        /// <summary>
        /// POST /printFarm/repeat?index={index}
        /// Repeat a job file from the queue
        /// </summary>
        /// <param name="index">Job index to repeat</param>
        /// <returns>
        /// HTTP status code:
        /// (204) No Content
        /// (400) Invalid parameters
        /// (500) Generic error occurred
        /// </returns>
        [HttpPost("repeat")]
        public async Task<IActionResult> RepeatFile(int index)
        {
            try
            {
                using (await _jobQueue.LockAsync())
                {
                    if (_jobQueue.Repeat(index))
                    {
                        return NoContent();
                    }
                    return BadRequest();
                }
            }
            catch (Exception e)
            {
                if (e is AggregateException ae)
                {
                    e = ae.InnerException;
                }
                _logger.LogWarning(e, $"[{nameof(RepeatFile)}] Failed repeat file #{index}");
                return StatusCode(500, e.Message);
            }
        }

        /// <summary>
        /// DELETE /printFarm/job?index={index}
        /// Remove a job file from the queue
        /// </summary>
        /// <param name="filename">Filename to remove</param>
        /// <param name="filename">Job index to remove</param>
        /// <returns>
        /// HTTP status code:
        /// (204) No Content
        /// (400) Invalid parameters
        /// (500) Generic error occurred
        /// </returns>
        [HttpDelete("job")]
        public async Task<IActionResult> RemoveFile(int index)
        {
            try
            {
                using (await _jobQueue.LockAsync())
                {
                    if ( _jobQueue.Remove(index))
                    {
                        return NoContent();
                    }
                    return BadRequest();
                }
            }
            catch (Exception e)
            {
                if (e is AggregateException ae)
                {
                    e = ae.InnerException;
                }
                _logger.LogWarning(e, $"[{nameof(RepeatFile)}] Failed remove file #{index}");
                return StatusCode(500, e.Message);
            }
        }

        /// <summary>
        /// GET /printFarm/cleanUp
        /// Clean up finished print jobs
        /// </summary>
        /// <returns>
        /// HTTP status code:
        /// (204) No Content
        /// </returns>
        [HttpPost("cleanUp")]
        public async Task<IActionResult> CleanUp()
        {
            using (await _jobQueue.LockAsync())
            {
                _jobQueue.Clean();
            }
            return NoContent();
        }
        #endregion

        #region Printer Requests
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
            using (await _printerList.LockAsync())
            {
                return Content(_printerList.ToJson(), "application/json");
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
                using (await _printerList.LockAsync())
                {
                    _printerList.Add(hostname);
                }
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
        /// POST /printFarm/suspendPrinter?hostname={hostname}
        /// Suspend a printer
        /// </summary>
        /// <returns>
        /// HTTP status code:
        /// (204) No Content
        /// (400) Invalid parameters
        /// (500) Generic error occurred
        /// </returns>
        [HttpPost("suspendPrinter")]
        public async Task<IActionResult> SuspendPrinter(string hostname)
        {
            if (string.IsNullOrWhiteSpace(hostname))
            {
                return BadRequest();
            }

            try
            {
                using (await _printerList.LockAsync())
                {
                    _printerList.Suspend(hostname);
                }
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
        /// POST /printFarm/resumePrinter?hostname={hostname}
        /// Resume normal printer operation
        /// </summary>
        /// <returns>
        /// HTTP status code:
        /// (204) No Content
        /// (400) Invalid parameters
        /// (500) Generic error occurred
        /// </returns>
        [HttpPost("resumePrinter")]
        public async Task<IActionResult> ResumePrinter(string hostname)
        {
            if (string.IsNullOrWhiteSpace(hostname))
            {
                return BadRequest();
            }

            try
            {
                using (await _printerList.LockAsync())
                {
                    _printerList.Resume(hostname);
                }
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
                using (await _printerList.LockAsync())
                {
                    _printerList.Remove(hostname);
                }
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
        #endregion
    }
}
