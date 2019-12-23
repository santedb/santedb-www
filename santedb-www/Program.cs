﻿/*
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
using SanteDB.Core.Configuration;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Model.Security;
using SanteDB.DisconnectedClient.Core.Configuration;
using SanteDB.DisconnectedClient.UI;
using SanteDB.DisconnectedClient.Xamarin;
using SanteDB.DisconnectedClient.Xamarin.Diagnostics;
using SanteDB.DisconnectedClient.Xamarin.Security;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace santedb_www
{
    /// <summary>
    /// Main SanteDB WWW Disconnected Server
    /// </summary>
    [Guid("A97FB5DE-7627-401C-8E70-5B96C1A0073B")]
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(String[] args)
        {

         
            // Output main header
            var parser = new ParameterParser<ConsoleParameters>();
            var parms = parser.Parse(args);
            parms.InstanceName = String.IsNullOrEmpty(parms.InstanceName) ? "default" : parms.InstanceName;
            
            // Output copyright info
            var entryAsm = Assembly.GetEntryAssembly();
            Console.WriteLine("SanteDB Disconnected Server (SanteDB) {0} ({1})", entryAsm.GetName().Version, entryAsm.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion);
            Console.WriteLine("{0}", entryAsm.GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright);
            Console.WriteLine("Complete Copyright information available at http://github.com/santedb/santedb-www");

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

                // Security Application Information
                var applicationIdentity = new SecurityApplication()
                {
                    ApplicationSecret = parms.ApplicationSecret ?? "SDB$$DEFAULT$$APPSECRET",
                    Name = parms.ApplicationName ?? "org.santedb.disconnected_client"
                };

                // Setup basic parameters
                String[] directory = {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SanteDBWWW", parms.InstanceName),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SanteDBWWW", parms.InstanceName)
                };

                foreach (var dir in directory)
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                // Token validator
                TokenValidationManager.SymmetricKeyValidationCallback += (o, k, i) =>
                {
                    Trace.TraceError("Trust issuer {0} failed", i);
                    return false;
                };
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


                if (parms.ShowHelp)
                    parser.WriteHelp(Console.Out);
                else if (parms.ConsoleMode)
                {
#if DEBUG
                    Tracer.AddWriter(new LogTraceWriter(System.Diagnostics.Tracing.EventLevel.LogAlways, "SanteDB.data"), System.Diagnostics.Tracing.EventLevel.LogAlways);
#else
                    Tracer.AddWriter(new LogTraceWriter(System.Diagnostics.Tracing.EventLevel.LogAlways, "SanteDB.data"), System.Diagnostics.Tracing.EventLevel.LogAlways);
#endif

                    XamarinApplicationContext.ProgressChanged += (o, e) =>
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine(">>> PROGRESS >>> {0} : {1:#0%}", e.ProgressText, e.Progress);
                        Console.ResetColor();
                    };

                    if (!DcApplicationContext.StartContext(new ConsoleDialogProvider(), $"www-{parms.InstanceName}", applicationIdentity))
                        DcApplicationContext.StartTemporary(new ConsoleDialogProvider(), $"www-{parms.InstanceName}", applicationIdentity);

                    DcApplicationContext.Current.Configuration.GetSection<ApplicationServiceContextConfigurationSection>().AppSettings.RemoveAll(o => o.Key == "http.bypassMagic");
                    DcApplicationContext.Current.Configuration.GetSection<ApplicationServiceContextConfigurationSection>().AppSettings.Add(new AppSettingKeyValuePair() { Key = "http.bypassMagic", Value = DcApplicationContext.Current.ExecutionUuid.ToString() });
                    Console.WriteLine("Press [Enter] key to close...");
                    Console.ReadLine();
                    DcApplicationContext.Current.Stop();

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
                else
                {
                    Trace.TraceInformation("Starting as Windows Service");
                    ServiceBase[] ServicesToRun;
                    ServicesToRun = new ServiceBase[]
                    {
                        new SanteDbService(parms.InstanceName, applicationIdentity)
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
                Trace.TraceError("Error encountered: {0}. Will terminate", e.Message);
#endif
                Environment.Exit(911);
            }
        }
    }
}
