using MARC.HI.EHRS.SVC.Core;
using SanteDB.Core.Model.Security;
using SanteDB.DisconnectedClient.Core.Configuration;
using SanteDB.DisconnectedClient.UI;
using SanteDB.DisconnectedClient.Xamarin;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace santedb_www
{
    public partial class SanteDbService : ServiceBase
    {
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
        /// When implemented in a derived class, executes when a Start command is sent to the service by the Service Control Manager (SCM) or when the operating system starts (for a service that starts automatically). Specifies actions to take when the service starts.
        /// </summary>
        /// <param name="args">Data passed by the start command.</param>
        protected override void OnStart(string[] args)
        {
            XamarinApplicationContext.ProgressChanged += (o, e) =>
            {
                Trace.TraceInformation(">>> PROGRESS >>> {0} : {1:#0%}", e.ProgressText, e.Progress);
            };

            if (!DcApplicationContext.StartContext(new ConsoleDialogProvider(), $"www-{this.ServiceName}", this.m_applicationIdentity))
                DcApplicationContext.StartTemporary(new ConsoleDialogProvider(), $"www-{this.ServiceName}", this.m_applicationIdentity);

            DcApplicationContext.Current.Configuration.GetSection<ApplicationConfigurationSection>().AppSettings.RemoveAll(o => o.Key == "http.bypassMagic");
            DcApplicationContext.Current.Configuration.GetSection<ApplicationConfigurationSection>().AppSettings.Add(new AppSettingKeyValuePair() { Key = "http.bypassMagic", Value = DcApplicationContext.Current.ExecutionUuid.ToString() });
        }

        /// <summary>
        /// When implemented in a derived class, executes when a Stop command is sent to the service by the Service Control Manager (SCM). Specifies actions to take when a service stops running.
        /// </summary>
        protected override void OnStop()
        {
            DcApplicationContext.Current.Stop();
        }
    }
}
