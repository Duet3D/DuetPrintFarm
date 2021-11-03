using System;
using System.IO;

namespace DuetPrintFarm.Model
{
    /// <summary>
    /// Class representing a queued job
    /// </summary>
    public class Job
    {
        /// <summary>
        /// Absolute filename of this job
        /// </summary>
        public string AbsoluteFilename
        {
            get => _absoluteFilename;
            set
            {
                Filename = Path.GetFileName(value);
                TimeCreated = File.Exists(value) ? File.GetLastWriteTime(value) : DateTime.Now;
                _absoluteFilename = value;
            }
        }
        private string _absoluteFilename;

        /// <summary>
        /// Plain filename of this job
        /// </summary>
        public string Filename { get; private set; }

        /// <summary>
        /// Hostname of the machine where this file is or was printed
        /// </summary>
        public string Hostname { get; set; }

        /// <summary>
        /// Datetime when this job was enqueued
        /// </summary>
        public DateTime TimeCreated { get; private set; }

        /// <summary>
        /// Progress label
        /// </summary>
        public string ProgressText { get; set; }

        /// <summary>
        /// File progress (in per cent, 0..1)
        /// </summary>
        public double? Progress { get; set; }

        /// <summary>
        /// Whether the print has been paused
        /// </summary>
        public bool Paused { get; set; }

        /// <summary>
        /// Whether the print has been cancelled
        /// </summary>
        public bool Cancelled { get; set; }

        /// <summary>
        /// Time left of this job (in s or null)
        /// </summary>
        public long? TimeLeft { get; set; }

        /// <summary>
        /// Indicates when the print job completed
        /// </summary>
        public DateTime? TimeCompleted { get; set; }

        /// <summary>
        /// Reset this instance so it can be printed again
        /// </summary>
        public void Reset()
        {
            Hostname = null;
            ProgressText = null;
            Progress = null;
            Paused = Cancelled = false;
            TimeLeft = null;
            TimeCompleted = null;
        }
    }
}
