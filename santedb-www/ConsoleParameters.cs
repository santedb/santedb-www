using MohawkCollege.Util.Console.Parameters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace santedb_www
{
    /// <summary>
    /// Represents console parameters for this particular instance
    /// </summary>
    public class ConsoleParameters
    {
        // <summary>
        /// When true, parameters should be shown
        /// </summary>
        [Description("Shows help and exits")]
        [Parameter("?")]
        [Parameter("help")]
        public bool ShowHelp { get; set; }

        /// <summary>
        /// When true console mode should be enabled
        /// </summary>
        [Description("Instructs the host process to run in console mode")]
        [Parameter("c")]
        [Parameter("console")]
        public bool ConsoleMode { get; set; }

        /// <summary>
        /// Gets or sets the name of the instance that is running
        /// </summary>
        [Description("Identifies the name of the instance to start")]
        [Parameter("n")]
        [Parameter("name")]
        public string InstanceName { get; set; }

        /// <summary>
        /// Installs the service
        /// </summary>
        [Description("Installs the service")]
        [Parameter("i")]
        [Parameter("install")]
        public bool Install { get; set; }

        /// <summary>
        /// Installs the service
        /// </summary>
        [Description("Uninstalls the service")]
        [Parameter("u")]
        [Parameter("uninstall")]
        public bool Uninstall { get; set; }

        /// <summary>
        /// Reset the service installation
        /// </summary>
        [Description("Resets the configuration of this WWW instance to default")]
        [Parameter("r")]
        [Parameter("reset")]
        public bool Reset { get; set; }

        /// <summary>
        /// Set the application name
        /// </summary>
        [Description("Sets the identity of the application (for OAUTH) for this instance")]
        [Parameter("appname")]
        public String ApplicationName { get; set; }

        /// <summary>
        /// The application secret
        /// </summary>
        [Description("Sets the secret of the application (for OAUTH) for this instance")]
        [Parameter("appsecret")]
        public String ApplicationSecret { get; set; }

    }
}
