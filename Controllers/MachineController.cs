﻿using System;
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
        /// Logger instance
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Job queue
        /// </summary>
        private readonly IJobQueue _jobQueue;

        /// <summary>
        /// Create a new controller instance
        /// </summary>
        /// <param name="configuration">Launch configuration</param>
        /// <param name="logger">Logger instance</param>
        /// <param name="jobQueue">Job queue instance</param>
        public MachineController(IConfiguration configuration, ILogger<MachineController> logger, IJobQueue jobQueue)
        {
            _configuration = configuration;
            _logger = logger;
            _jobQueue = jobQueue;
        }

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
            return Path.Combine(GCodesDirectory, path);
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
            filename = HttpUtility.UrlDecode(filename);

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
            filename = HttpUtility.UrlDecode(filename);

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
                _logger.LogWarning(e, $"[{nameof(UploadFile)} Failed upload file {filename} (resolved to {resolvedPath})");
                return StatusCode(500, e.Message);
            }
        }
        #endregion
    }
}
