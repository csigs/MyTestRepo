namespace Juno.Payload.Client
{
    using System;
    using System.Net.Http;

    using Juno.Payload.Client.Abstraction.V1;
    using Juno.Payload.Client.Configuration;

    using Microsoft.Localization.DelegatingHandlers;
    using Microsoft.Localization.RestApiAccess;

    //[Obsolete]
    public class PayloadClientFactory<TInlineMetadata, TMetadata> : IPayloadClientFactory<TInlineMetadata, TMetadata>
        where TInlineMetadata : class
        where TMetadata : class
    {
        private readonly PayloadClientConfig _payloadClientConfig;

        private readonly IRestfulApiAccessProvider _restfulApiAccessProvider;

        public PayloadClientFactory(PayloadClientConfig payloadClientConfig)
        {
            _payloadClientConfig = payloadClientConfig ?? throw new ArgumentNullException(nameof(payloadClientConfig));
            _restfulApiAccessProvider = new RestfulApiAccessProvider(
                new(new ExponentialBackoffRetryDelegatingHandler(
                    new BearerTokenDelegatingHandler(new[] { payloadClientConfig.MSIScope }, new HttpClientHandler())
                    )));
        }

        public IPayloadClient<TInlineMetadata, TMetadata> Create(Guid payloadId, Guid? fallbackBuildId = null)
        {
            return new PayloadClient<TInlineMetadata, TMetadata>(payloadId, fallbackBuildId, _payloadClientConfig, _restfulApiAccessProvider);
        }

        public IPayloadReadClient<TInlineMetadata, TMetadata> CreateReadClient(Guid payloadId, Guid? fallbackBuildId = null)
        {
            return Create(payloadId, fallbackBuildId);
        }
    }
}
