using SanteDB.Client;
using SanteDB.Core;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace santedb_www
{
    /// <summary>
    /// Debugger application context
    /// </summary>
    internal class WebApplicationContext : ClientApplicationContextBase
    {

        // The original console parameters
        private ConsoleParameters m_consoleParameters;

        /// <inheritdoc/>
        public WebApplicationContext(ConsoleParameters debugParameters, IConfigurationManager configurationManager) : base(SanteDBHostType.Gateway, debugParameters.InstanceName, configurationManager)
        {
            this.m_consoleParameters = debugParameters;
        }

        /// <inheritdoc/>
        protected override void OnRestartRequested(object sender)
        {
            // Delay fire - allow other objects to finish up on the restart request event

            Thread.Sleep(1000);
            ServiceUtil.Stop();

            if (this.m_consoleParameters.ConsoleMode)
            {
                var pi = new ProcessStartInfo(typeof(Program).Assembly.Location, string.Join(" ", this.m_consoleParameters.ToArgumentList()));
                Process.Start(pi);
            }
            else
            {
                var pi = new ProcessStartInfo(typeof(Program).Assembly.Location, $"--restart --name={this.m_consoleParameters.InstanceName}");
                Process.Start(pi);
            }
            Thread.Sleep(1000);

        }
    }
}
