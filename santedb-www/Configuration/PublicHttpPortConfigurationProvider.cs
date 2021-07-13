using SanteDB.Core.Configuration;
using SanteDB.DisconnectedClient.Ags.Configuration;
using SanteDB.DisconnectedClient.Configuration;
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
    public class PublicHttpPortConfigurationProvider : IInitialConfigurationProvider
    {
        /// <summary>
        /// Apply the configuration
        /// </summary>
        public SanteDBConfiguration Provide(SanteDBConfiguration existing)
        {
            var agsSection = existing.GetSection<AgsConfigurationSection>();
            if(agsSection != null)
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
