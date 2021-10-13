using System;
using System.IO;
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
    /// MVC Controller for /machine requests
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class MachineController : ControllerBase
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
        /// Convert a RRF/FatFS path to a G-code file path
        /// </summary>
        /// <param name="path">RRF-style path</param>
        /// <returns>Converted file path</returns>
        private string ConvertFirmwarePath(string path)
        {
            // Strip "0:/" or just "/"
            for (int i = 0; i < path.Length; i++)
            {
                if (!char.IsNumber(path[i]) && path[i] != ':' && path[i] != '/')
                {
                    path = path[i..];
                    break;
                }
            }

            // Strip "gcodes/"
            if (path.StartsWith("gcodes/"))
            {
                path = path["gcodes/".Length..];
            }

            // Return combined path
            string gcodesDirectory = _configuration.GetValue<string>("gcodesDirectory");
            return Path.Combine(gcodesDirectory, path);
        }

        /// <summary>
        /// Logger instance
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Create a new controller instance
        /// </summary>
        /// <param name="configuration">Launch configuration</param>
        /// <param name="logger">Logger instance</param>
        public MachineController(IConfiguration configuration, ILogger<MachineController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        #region File requests
        /// <summary>
        /// GET /machine/file/{filename}
        /// Download the specified file.
        /// </summary>
        /// <param name="filename">File to download</param>
        /// <returns>
        /// HTTP status code:
        /// (200) File content
        /// (404) File not found
        /// (500) Generic error
        /// </returns>
        [HttpGet("file/{*filename}")]
        public IActionResult DownloadFile(string filename)
        {
            string resolvedPath = "n/a";
            try
            {
                resolvedPath = ConvertFirmwarePath(filename);
                if (!System.IO.File.Exists(resolvedPath))
                {
                    _logger.LogWarning($"[{nameof(DownloadFile)}] Could not find file {filename} (resolved to {resolvedPath})");
                    return NotFound(HttpUtility.UrlPathEncode(filename));
                }

                FileStream stream = new(resolvedPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                return File(stream, "application/octet-stream");
            }
            catch (Exception e)
            {
                if (e is AggregateException ae)
                {
                    e = ae.InnerException;
                }
                _logger.LogWarning(e, $"[{nameof(DownloadFile)}] Failed download file {filename} (resolved to {resolvedPath})");
                return StatusCode(500, e.Message);
            }
        }

        /// <summary>
        /// PUT /machine/file/{filename}
        /// Upload a file from the HTTP body and create the subdirectories if necessary.
        /// </summary>
        /// <param name="filename">Destination of the file to upload</param>
        /// <returns>
        /// HTTP status code:
        /// (201) File created
        /// (500) Generic error occurred
        /// </returns>
        [DisableRequestSizeLimit]
        [HttpPut("file/{*filename}")]
        public async Task<IActionResult> UploadFile(string filename)
        {
            string resolvedPath = "n/a";
            try
            {
                resolvedPath = ConvertFirmwarePath(filename);

                // Create directory if necessary
                string directory = Path.GetDirectoryName(resolvedPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

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
                _logger.LogWarning(e, $"[{nameof(UploadFile)} Failed upload file {filename} (resolved to {resolvedPath})");
                return StatusCode(500, e.Message);
            }
        }
        #endregion
    }
}
