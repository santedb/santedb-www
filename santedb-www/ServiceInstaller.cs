using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.Threading.Tasks;

namespace santedb_www
{
    [RunInstaller(true)]
    public class ServiceInstaller : System.Configuration.Install.Installer
    {
        /// <summary>
		/// The m service process installer.
		/// </summary>
		private System.ServiceProcess.ServiceProcessInstaller m_serviceProcessInstaller;

        /// <summary>
        /// The m service installer.
        /// </summary>
        private System.ServiceProcess.ServiceInstaller m_serviceInstaller;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceInstaller"/> class.
        /// </summary>
        public ServiceInstaller(string instanceName)
        {
            // This call is required by the Designer.
            InitializeComponent(instanceName);
        }

        /// <summary>
        /// Initializes the component.
        /// </summary>
        private void InitializeComponent(string instanceName)
        {
            this.m_serviceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.m_serviceInstaller = new System.ServiceProcess.ServiceInstaller();

            this.m_serviceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.m_serviceProcessInstaller.Password = null;
            this.m_serviceProcessInstaller.Username = null;
            this.m_serviceInstaller.ServiceName = $"sdb-www-{instanceName}";
            this.m_serviceInstaller.Description = "Provides the execution environment for SanteDB";
            this.m_serviceInstaller.DelayedAutoStart = true;
            this.m_serviceInstaller.DisplayName = $"SanteDB Disconnected Client ({instanceName})";
            this.m_serviceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;

            this.Installers.AddRange(
                new System.Configuration.Install.Installer[]
                {
                    this.m_serviceProcessInstaller,
                    this.m_serviceInstaller
                });
        }
    }
}
