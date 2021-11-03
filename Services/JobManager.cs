using DuetPrintFarm.Singletons;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DuetPrintFarm.Services
{
    /// <summary>
    /// Main service to schedule jobs for the different printers
    /// </summary>
    public sealed class JobManager : IHostedService
    {
        /// <summary>
        /// Configuration of this application
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
        /// Constructor of this service class
        /// </summary>
        /// <param name="configuration">App configuration</param>
        /// <param name="logger">Logger instance</param>
        /// <param name="jobQueue">Job queue</param>
        public JobManager(IConfiguration configuration, ILogger<JobManager> logger, IJobQueue jobQueue)
        {
            _logger = logger;
            _configuration = configuration;
            _jobQueue = jobQueue;
        }

        /// <summary>
        /// File where the job queue is stored
        /// </summary>
        private string JobQueueFile { get => _configuration.GetValue<string>("JobQueueFile"); }

        /// <summary>
        /// Start this service
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Asynchronous task</returns>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (File.Exists(JobQueueFile))
            {
                using (await _jobQueue.LockAsync(cancellationToken))
                {
                    await _jobQueue.LoadFromFileAsync(JobQueueFile, cancellationToken);
                }
                _logger.LogInformation("Jobs loaded from {0}", JobQueueFile);
            }
        }

        /// <summary>
        /// Stop this service
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Asynchronous task</returns>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            using (await _jobQueue.LockAsync(cancellationToken))
            {
                await _jobQueue.SaveToFileAsync(JobQueueFile, cancellationToken);
            }
            _logger.LogInformation("Jobs saved to {0}", JobQueueFile);
        }
    }
}
