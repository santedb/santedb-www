﻿using SanteDB.Client;
using SanteDB.Client.Configuration;
using SanteDB.Core;
using SanteDB.Core.Applets.Services;
using SanteDB.Core.Diagnostics;
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
        private Tracer m_tracer = Tracer.GetTracer(typeof(WebApplicationContext));
        private readonly IConfigurationManager m_configurationManager;
        private bool m_restartRequested = false;

        /// <inheritdoc/>
        public WebApplicationContext(ConsoleParameters debugParameters, IConfigurationManager configurationManager) : base(SanteDBHostType.Gateway, debugParameters.InstanceName, configurationManager)
        {
            this.m_configurationManager = configurationManager;
            this.m_consoleParameters = debugParameters;
            this.Stopped += (o, e) =>
            {
                if (this.m_restartRequested)
                {
                    this.m_tracer.TraceWarning("Attempting to restart SanteDB-www host");
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
                }
            };
        }

        /// <inheritdoc/>
        protected override void OnRestartRequested(object sender)
        {
            // Delay fire - allow other objects to finish up on the restart request event
            this.m_restartRequested = true;

            if(this.m_configurationManager is InitialConfigurationManager &&
                this.m_configurationManager.GetSection<ClientConfigurationSection>().AutoUpdateApplets) // we need to uninstall the default applets and let the update do the magic
            {
                var appService = this.GetService<IAppletManagerService>();
                foreach(var app in appService.Applets.ToArray())
                {
                    appService.UnInstall(app.Info.Id);
                }
            }
            ServiceUtil.Stop();
            Environment.Exit(0);
        }
    }
}
