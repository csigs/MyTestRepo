namespace Juno.Payload.Client.Configuration
{
    using System;

    public partial class PayloadClientConfig
    {
        internal PayloadClientConfig()
        {
        }

        public PayloadClientConfig(string serviceUri, string msiScope)
        {
            if (string.IsNullOrWhiteSpace(serviceUri))
            {
                throw new ArgumentException($"'{nameof(serviceUri)}' cannot be null or whitespace.", nameof(serviceUri));
            }

            ServiceUri = serviceUri.TrimEnd('/', '\\');

            MSIScope = msiScope;
        }

        public string ServiceUri { get; set; }
        public string MSIScope { get; set; }

        public string GetServiceBaseUri()
        {
            return ServiceUri.TrimEnd('/', '\\');
        }
    }
}
