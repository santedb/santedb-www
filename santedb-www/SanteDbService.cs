/*
 * Portions Copyright 2015-2019 Mohawk College of Applied Arts and Technology
 * Portions Copyright 2019-2019 SanteSuite Contributors (See NOTICE)
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you
 * may not use this file except in compliance with the License. You may
 * obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations under
 * the License.
 *
 * User: Justin Fyfe
 * Date: 2019-8-8
 */

using SanteDB.Core.Model.Security;
using SanteDB.DisconnectedClient.Configuration;
using SanteDB.DisconnectedClient.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using SanteDB.Core.Configuration;
using SanteDB.Core;
using SanteDB.Core.Services;
using System.Security.Principal;
using SanteDB.DisconnectedClient;

namespace santedb_www
{
    public partial class SanteDbService : ServiceBase
    {
        private bool m_serviceStop = false;

        // THe application identity
        private SecurityApplication m_applicationIdentity;

        /// <summary>
        /// SanteDB Service
        /// </summary>
        public SanteDbService(string instanceName, SecurityApplication applicationIdentity)
        {
            InitializeComponent();
            this.m_applicationIdentity = applicationIdentity;
            this.ServiceName = instanceName;
        }

        /// <summary>
        /// Run the application host (moved here to allow for auto-restart)
        /// </summary>
        private void RunApplication(string[] args)
        {
            if (!DcApplicationContext.StartContext(new ConsoleDialogProvider(), $"www-{this.ServiceName}", this.m_applicationIdentity, SanteDBHostType.Other))
                DcApplicationContext.StartTemporary(new ConsoleDialogProvider(), $"www-{this.ServiceName}", this.m_applicationIdentity, SanteDBHostType.Other);
            DcApplicationContext.Current.Configuration.GetSection<ApplicationServiceContextConfigurationSection>().AppSettings.RemoveAll(o => o.Key == "http.bypassMagic");
            DcApplicationContext.Current.Configuration.GetSection<ApplicationServiceContextConfigurationSection>().AppSettings.Add(new AppSettingKeyValuePair() { Key = "http.bypassMagic", Value = DcApplicationContext.Current.ExecutionUuid.ToString() });
        }

        /// <summary>
        /// When implemented in a derived class, executes when a Start command is sent to the service by the Service Control Manager (SCM) or when the operating system starts (for a service that starts automatically). Specifies actions to take when the service starts.
        /// </summary>
        /// <param name="args">Data passed by the start command.</param>
        protected override void OnStart(string[] args)
        {
            try
            {
                ApplicationContext.ProgressChanged += (o, e) =>
                {
                    Trace.TraceInformation(">>> PROGRESS >>> {0} : {1:#0%}", e.ProgressText, e.Progress);
                };

                RunApplication(args);

                ApplicationServiceContext.Current.Stopped += (o, e) =>
                {
                    if (!this.m_serviceStop)
                    {
                        var pi = new ProcessStartInfo(typeof(Program).Assembly.Location, $"--restart --name={this.ServiceName}");
                        Process.Start(pi);
                        Environment.Exit(0);
                    }
                };
            }
            catch (Exception e)
            {
                Trace.TraceError("The service reported an error: {0}", e);
                EventLog.WriteEntry("SanteDB Portal Process", $"Service Startup reported an error: {e}", EventLogEntryType.Error, 1911);
                Environment.FailFast($"Error starting WWW service: {e.Message}");
            }
        }

        /// <summary>
        /// When implemented in a derived class, executes when a Stop command is sent to the service by the Service Control Manager (SCM). Specifies actions to take when a service stops running.
        /// </summary>
        protected override void OnStop()
        {
            try
            {
                Trace.TraceInformation("Stopping Service");
                this.m_serviceStop = true;
                DcApplicationContext.Current.Stop();
            }
            catch (Exception e)
            {
                EventLog.WriteEntry("SanteDB Portal Process", $"Service Shutdown reported an error: {e}", EventLogEntryType.Error, 1911);
                Trace.TraceError("The service reported an error: {0}", e);
                Environment.FailFast($"Error stopping WWW service: {e.Message}");
            }
        }
    }
}