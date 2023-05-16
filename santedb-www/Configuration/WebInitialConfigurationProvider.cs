﻿using SanteDB;
using SanteDB.Client.Configuration;
using SanteDB.Client.UserInterface;
using SanteDB.Core;
using SanteDB.Core.Configuration;
using SanteDB.Rest.Common.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace santedb_www.Configuration
{
    /// <summary>
    /// Opens the initial ports to public configuration port
    /// </summary>
    public class WebInitialConfigurationProvider : IInitialConfigurationProvider
    {
        /// <summary>
        /// Order of the apply of this configuration provider
        /// </summary>
        public int Order => Int32.MaxValue;

        /// <summary>
        /// Apply the configuration
        /// </summary>
        public SanteDBConfiguration Provide(SanteDBHostType hostContextType, SanteDBConfiguration existing)
        {
            var appSection = existing.GetSection<ApplicationServiceContextConfigurationSection>();
            appSection.ServiceProviders.RemoveAll(o => o.Type.Implements(typeof(IUserInterfaceInteractionProvider)));

            appSection.ServiceProviders.Add(new TypeReferenceConfiguration(typeof(SanteDB.Client.UserInterface.Impl.TracerUserInterfaceInteractionProvider)));
            appSection.ServiceProviders.Add(new TypeReferenceConfiguration(typeof(SanteDB.Client.UserInterface.WebAppletHostBridgeProvider)));
            var agsSection = existing.GetSection<RestConfigurationSection>();
            if (agsSection != null)
            {
#if !DEBUG
                foreach(var service in agsSection.Services)
                {
                    foreach(var endpooint in service.Endpoints)
                    {
                        endpooint.Address = endpooint.Address.Replace("127.0.0.1", "0.0.0.0");
                    }
                }
#endif
            }
            return existing;
        }

    }
}
