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

using MohawkCollege.Util.Console.Parameters;
using Mono.Unix;
using Mono.Unix.Native;
using SanteDB.Client.Batteries;
using SanteDB.Client.Configuration;
using SanteDB.Client.Configuration.Upstream;
using SanteDB.Client.Rest;
using SanteDB.Core;
using SanteDB.Core.Configuration;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Diagnostics.Tracing;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Services;
using SanteDB.Core.Services.Impl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace santedb_www
{
    /// <summary>
    /// Main SanteDB WWW Disconnected Server
    /// </summary>
    [Guid("A97FB5DE-7627-401C-8E70-5B96C1A0073B")]
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        private static void Main(String[] args)
        {
            // Output main header
            var parser = new ParameterParser<ConsoleParameters>();
            var parms = parser.Parse(args);
            parms.InstanceName = String.IsNullOrEmpty(parms.InstanceName) ? "default" : parms.InstanceName;

            // Output copyright info
            var entryAsm = Assembly.GetEntryAssembly();
            Console.WriteLine("SanteDB Disconnected Web Host (SanteDB) {0} ({1})", entryAsm.GetName().Version, entryAsm.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion);
            Console.WriteLine("{0}", entryAsm.GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright);
            Console.WriteLine("Complete Copyright information available at http://github.com/santedb/santedb-www");

            // Parameters to force load?
            var dllFiles = Directory.GetFiles(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Sante*.dll");
            if (parms.Force)
            {
                dllFiles = dllFiles.Union(Directory.GetFiles(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "*.dll")).ToArray();
            }

            foreach (var itm in dllFiles)
            {
                try
                {
                    var asm = Assembly.LoadFile(itm);
                    Console.WriteLine("Force Loaded {0}", asm.FullName);
                }
                catch (Exception e)
                {
                    Console.WriteLine("ERR: Cannot load {0} due to {1}", itm, e.Message);
                }
            }

            AppDomain.CurrentDomain.AssemblyResolve += (o, e) =>
            {
                string pAsmName = e.Name;
                if (pAsmName.Contains(","))
                    pAsmName = pAsmName.Substring(0, pAsmName.IndexOf(","));

                var asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => e.Name == a.FullName) ??
                    AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => pAsmName == a.GetName().Name);
                return asm;
            };

            try
            {
                // Detect platform
                if (System.Environment.OSVersion.Platform != PlatformID.Win32NT)
                    Trace.TraceWarning("Not running on WindowsNT, some features may not function correctly");
                else if (!EventLog.SourceExists("SanteDB"))
                    EventLog.CreateEventSource("SanteDB", "santedb-www");

                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, error) =>
                {
                    if (certificate == null || chain == null)
                        return false;
                    else
                    {
                        if (chain.ChainStatus.Length > 0 || error != SslPolicyErrors.None)
                        {
                            Trace.TraceWarning("The remote certificate is not trusted. The error was {0}. The certificate is: \r\n{1}", error, certificate.Subject);
                            return false;
                        }
                        return true;
                    }
                };

                // Different binding port?
                if (String.IsNullOrEmpty(parms.BaseUrl))
                {
                    parms.BaseUrl = "http://127.0.0.1:9200";
                }

                AppDomain.CurrentDomain.SetData(RestServiceInitialConfigurationProvider.BINDING_BASE_DATA, parms.BaseUrl);


                if (parms.ShowHelp)
                    parser.WriteHelp(Console.Out);
                else if (parms.Reset)
                {
                    var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SanteDB", parms.InstanceName);
                    var cData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SanteDB", parms.InstanceName);
                    if (Directory.Exists(appData)) Directory.Delete(cData, true);
                    if (Directory.Exists(appData)) Directory.Delete(appData, true);
                    Console.WriteLine("Environment Reset Successful");
                    return;
                }
                else if (parms.ConsoleMode)
                {
#if DEBUG
                    Tracer.AddWriter(new ConsoleTraceWriter(EventLevel.Informational, "SanteDB.Data", new Dictionary<String, EventLevel>()), EventLevel.Informational);
#else
                    Tracer.AddWriter(new ConsoleTraceWriter(EventLevel.Warning, "SanteDB.Data", new Dictionary<String, EventLevel>()), EventLevel.Warning);
#endif
                    Trace.Listeners.Add(new ConsoleTraceListener());

                    var context = CreateContext(parms);

                    ServiceUtil.Start(Guid.NewGuid(), context);

                    if (!parms.Forever)
                    {
                        Console.WriteLine("Press [Enter] key to close...");
                        Console.ReadLine();
                    }
                    else
                    {
                        Console.WriteLine("Will run in nohup daemon mode...");
                        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                        {
                            // Wait until cancel key is pressed
                            var mre = new ManualResetEventSlim(false);
                            // Application requested a restart
                            ApplicationServiceContext.Current.Stopped += (o, e) => mre.Set();
                            // User requested a stop
                            Console.CancelKeyPress += (o, e) => mre.Set();
                            mre.Wait();
                        }
                        else
                        {  // running on unix
                           // Now wait until the service is exiting va SIGTERM or SIGSTOP
                            UnixSignal[] signals = new UnixSignal[]
                            {
                                new UnixSignal(Mono.Unix.Native.Signum.SIGINT),
                                new UnixSignal(Mono.Unix.Native.Signum.SIGTERM),
                                new UnixSignal(Mono.Unix.Native.Signum.SIGQUIT),
                                new UnixSignal(Mono.Unix.Native.Signum.SIGHUP)
                            };

                            ApplicationServiceContext.Current.Stopped += (o, e) =>
                            {
                                Console.WriteLine("Service has stopped, will send SIGHUP to self for restart");
                                Syscall.kill(Syscall.getpid(), Signum.SIGHUP);
                            };

                            Console.WriteLine("Started - Send SIGINT, SIGTERM, SIGQUIT or SIGHUP to PID {0} to terminate", Process.GetCurrentProcess().Id);
                            int signal = UnixSignal.WaitAny(signals);
                        }
                    }

                    Console.WriteLine($"Received termination signal...");
                    ServiceUtil.Stop();

                }
                else if (parms.Install)
                {
                    string serviceName = $"sdb-www-{parms.InstanceName}";
                    if (!ServiceTools.ServiceInstaller.ServiceIsInstalled(serviceName))
                    {
                        String argList = String.Empty;
                        if (!String.IsNullOrEmpty(parms.ApplicationName))
                            argList += $" --appname=\"{parms.ApplicationName}\"";
                        if (!String.IsNullOrEmpty(parms.ApplicationSecret))
                            argList += $" --appsecret=\"{parms.ApplicationSecret}\"";

                        ServiceTools.ServiceInstaller.Install(
                            serviceName, $"SanteDB WWW ({parms.InstanceName})",
                            $"{entryAsm.Location} --name=\"{parms.InstanceName}\" {argList}",
                            null, null, ServiceTools.ServiceBootFlag.AutoStart);
                    }
                    else
                        throw new InvalidOperationException("Service instance already installed");
                }
                else if (parms.Uninstall)
                {
                    string serviceName = $"sdb-www-{parms.InstanceName}";
                    if (ServiceTools.ServiceInstaller.ServiceIsInstalled(serviceName))
                        ServiceTools.ServiceInstaller.Uninstall(serviceName);
                    else
                        throw new InvalidOperationException("Service instance not installed");
                }
                else if (parms.Restart)
                {
                    string serviceName = $"sdb-www-{parms.InstanceName}";
                    if (ServiceTools.ServiceInstaller.ServiceIsInstalled(serviceName))
                    {
                        Console.Write("Stopping {0}...", serviceName);
                        var niter = 0;
                        ServiceTools.ServiceInstaller.StopService(serviceName);
                        while (ServiceTools.ServiceInstaller.GetServiceStatus(serviceName) != ServiceTools.ServiceState.Stop && niter < 10)
                        {
                            Thread.Sleep(1000);
                            Console.Write(".");
                            niter++;
                        }
                        Console.Write("\r\nStarting {0}...", serviceName);
                        ServiceTools.ServiceInstaller.StartService(serviceName);
                        while (ServiceTools.ServiceInstaller.GetServiceStatus(serviceName) != ServiceTools.ServiceState.Run && niter < 20)
                        {
                            Thread.Sleep(1000);
                            Console.Write(".");
                            niter++;
                        }
                        Console.WriteLine("Restart Complete");
                    }
                }
                else
                {
                    Trace.TraceInformation("Starting as Windows Service");
                    ServiceBase[] ServicesToRun;
                    ServicesToRun = new ServiceBase[]
                    {
                        new SanteDbService(parms.InstanceName, CreateContext(parms))
                    };
                    ServiceBase.Run(ServicesToRun);
                    Trace.TraceInformation("Started As Windows Service...");
                }
            }
            catch (Exception e)
            {
#if DEBUG
                Trace.TraceError("011 899 981 199 911 9725 3!!! {0}", e.ToString());
                Console.WriteLine("011 899 981 199 911 9725 3!!! {0}", e.ToString());
#else
                Trace.TraceError("Error encountered: {0}. Will terminate", e);
                EventLog.WriteEntry("SanteDB", $"Fatal service error: {e}", EventLogEntryType.Error, 911);
                Console.WriteLine("FATAL ERROR: {0}", e);
#endif
                Environment.Exit(911);
            }
        }

        /// <summary>
        /// Create context
        /// </summary>
        private static IApplicationServiceContext CreateContext(ConsoleParameters parms)
        {
            var configDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "santedb", "www", parms.InstanceName);
            // Does the config or data directory point to WinDir
            if(configDirectory.Contains(Environment.GetFolderPath(Environment.SpecialFolder.Windows)))
            {
                configDirectory = Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "instances", parms.InstanceName);
            }
            var dataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "santedb", "www", parms.InstanceName);
            if (dataDirectory.Contains(Environment.GetFolderPath(Environment.SpecialFolder.Windows)))
            {
                dataDirectory = Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "instances", parms.InstanceName);
            }

            if (!Directory.Exists(dataDirectory))
            {
                Directory.CreateDirectory(dataDirectory);
            }
            if (!Directory.Exists(configDirectory))
            {
                Directory.CreateDirectory(configDirectory);
            }

            // Security Application Information
            var applicationIdentity = new UpstreamCredentialConfiguration()
            {
                Conveyance = UpstreamCredentialConveyance.Secret,
                CredentialName = parms.ApplicationName ?? "org.santedb.disconnected_client",
                CredentialSecret = parms.ApplicationSecret ?? "SDB$$DEFAULT$$APPSECRET",
                CredentialType = UpstreamCredentialType.Application
            };

            ClientBatteries.Initialize(dataDirectory, configDirectory, applicationIdentity);
            // Establish a configuration environment 
            IConfigurationManager configurationManager = null;
            var configurationFile = Path.Combine(configDirectory, "santedb.config");
            if (File.Exists(configurationFile))
            {
                configurationManager = new FileConfigurationService(configurationFile, true);
            }
            else
            {
                configurationManager = new InitialConfigurationManager(SanteDBHostType.Gateway, parms.InstanceName, configurationFile);
            }


            return new WebApplicationContext(parms, configurationManager);
        }
    }
}