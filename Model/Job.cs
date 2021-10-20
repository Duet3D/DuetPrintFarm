using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

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
        public string AbsoluteFilename { get; set; }

        /// <summary>
        /// Plain filename of this job
        /// </summary>
        public string Filename { get => Path.GetFileName(AbsoluteFilename); }

        /// <summary>
        /// Hostname of the machine where this file is or was printed
        /// </summary>
        public string Hostname { get; set; }

        /// <summary>
        /// Datetime when this job was enqueued
        /// </summary>
        public DateTime TimeCreated { get => File.GetLastWriteTime(AbsoluteFilename); }

        /// <summary>
        /// File progress (in per cent, 0..1)
        /// </summary>
        public double? Progress { get; set; }

        /// <summary>
        /// Time left of this job (in s or null)
        /// </summary>
        public long? TimeLeft { get; set; }

        /// <summary>
        /// Indicates when the print job completed
        /// </summary>
        public DateTime? TimeCompleted { get; set; }
    }
}
