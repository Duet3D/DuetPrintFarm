using System;
using System.IO;
using System.Text.Json.Serialization;

namespace DuetPrintFarm.Model
{
    /// <summary>
    /// Class representing a queued job
    /// </summary>
    public class Job
    {
        /// <summary>
        /// Filename of this job
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// Plain filename of this job
        /// </summary>
        [JsonIgnore]
        public string ShortName { get => Path.GetFileName(Filename); }

        /// <summary>
        /// Datetime when this job was enqueued
        /// </summary>
        public DateTime TimeCreated { get; set; }
    }
}
