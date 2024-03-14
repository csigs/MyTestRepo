
namespace Juno.Payload.Service.Metrics
{
    using Juno.Payload.Service.Configurations;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Net;

    public class GenevaMetricDimensionProvider : IGenevaMetricDimensionProvider
    {
        private string CustomerId { get; }

        public GenevaMetricDimensionProvider(IConfigurationRoot configRoot)
        {
            this.CustomerId = configRoot[GenevaConfigurationSettings.CustomerResourceId] ?? throw new ArgumentException($"Couldn't find {GenevaConfigurationSettings.CustomerResourceId} in function configuration");
        }

        public string GetCustomerId()
        {
            return this.CustomerId;
        }

        public string GetInstance(string machineName)
        {
            var addresses = Dns.GetHostAddresses(machineName);
            string ipAddress = null;
            foreach (var address in addresses)
            {
                if (address.ToString() == "127.0.0.1")
                {
                    continue;
                }
                else if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    ipAddress = address.ToString();
                    break;
                }
            }
            return ipAddress;
        }
    }
}
