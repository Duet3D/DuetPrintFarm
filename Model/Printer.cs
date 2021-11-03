using DuetAPI.ObjectModel;

namespace DuetPrintFarm.Model
{
    /// <summary>
    /// Class representing a remote printer
    /// </summary>
    public class Printer
    {
        /// <summary>
        /// Name of this printer
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Hostname of this printer
        /// </summary>
        public string Hostname { get; set; }

        /// <summary>
        /// Current status of the machine
        /// </summary>
        public MachineStatus Status { get; set; }

        /// <summary>
        /// Indicates if this machine is online
        /// </summary>
        public bool Online { get; set; }

        /// <summary>
        /// Indicates if the machine is paused
        /// </summary>
        public bool Suspended { get; set; }

        /// <summary>
        /// Current job file being processed (may or may not be a valid job)
        /// </summary>
        public string JobFile { get; set; }

        /// <summary>
        /// Default constructor of this class
        /// </summary>
        public Printer() { }

        /// <summary>
        /// Constructor of this class
        /// </summary>
        /// <param name="hostname">Hostname of this machine</param>
        public Printer(string hostname)
        {
            Name = hostname;
            Hostname = hostname;
        }
    }
}
