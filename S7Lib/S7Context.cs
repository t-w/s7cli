using Serilog;

namespace S7Lib
{
    /// <summary>
    /// Context in which to run static S7 Library functions
    /// </summary>
    public class S7Context
    {
        /// <summary>
        /// Handle for the Simatic API
        /// </summary>
        public SimaticLib.Simatic Api;
        /// <summary>
        /// Handle for Serilog logger
        /// </summary>
        public Serilog.Core.Logger Log;

        // TODO: Review constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <remarks>
        /// Regarding automaticSave, as per official documentation:
        /// If enabled, the changes are saved immediately, especially for all operations that change structure
        /// (for methods such as Add, Copy, or Remove, with which objects are added or deleted)
        /// as well as for name changes.
        /// </remarks>
        /// <param name="_log">Configured logger object</param>
        /// <param name="serverMode">UnattandedServerMode surpress GUI messages</param>
        /// <param name="automaticSave">Save project automatically</param>
        public S7Context(Serilog.Core.Logger log=null, bool serverMode = true, bool automaticSave = true)
        {
            Api = new SimaticLib.Simatic
            {
                UnattendedServerMode = serverMode,
                AutomaticSave = automaticSave ? 1 : 0
            };

            if (log == null)
                log = CreateConsoleLogger();
            Log = log;
        }

        // TODO: Remove?
        private Serilog.Core.Logger CreateConsoleLogger()
        {
            return new LoggerConfiguration().MinimumLevel.Debug()
               .WriteTo.Console().CreateLogger();
        }
    }

}
