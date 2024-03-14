namespace Juno.Payload.Client
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using Juno.Payload.Client.Abstraction;
    using Juno.Payload.Client.Configuration;
    using Juno.Payload.Client.Extensions;
    using Juno.Payload.Dto;
    using Microsoft.Extensions.Primitives;
    using Microsoft.Localization.RestApiAccess;
    using Microsoft.Localization.SignProviders;
    using Newtonsoft.Json;

    public class PayloadClientV2 : IPayloadClientV2
    {
        internal static readonly TimeSpan DEFAULT_HTTP_TIMEOUT = TimeSpan.FromMinutes(5);

        private readonly PayloadClientConfig _config;
        private readonly ISignProvider _signProvider;
        private readonly IRestfulApiAccessProvider _restfulApiAccessProvider;

        public PayloadClientV2(string partitionKey, Guid payloadId, PayloadClientConfig config, IRestfulApiAccessProvider restfulApiAccessProvider, ISignProvider signProvider)
        {
            if (string.IsNullOrWhiteSpace(partitionKey))
            {
                throw new ArgumentException($"'{nameof(partitionKey)}' cannot be null or whitespace.", nameof(partitionKey));
            }

            if (payloadId == Guid.Empty)
            {
                throw new ArgumentException(nameof(payloadId), "Value can't be empty");
            }

            PayloadId = payloadId;
            PartitionKey = partitionKey;
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _restfulApiAccessProvider = restfulApiAccessProvider ??
                throw new ArgumentNullException(nameof(restfulApiAccessProvider));
            _signProvider = signProvider;
        }

        public Guid PayloadId { get; }

        public string PartitionKey { get; }

        public async Task<bool> ExistsAsync(CancellationToken cancellationToken = default)
        {
            var functionUrl = $"{_config.GetServiceBaseUri()}/api/{PayloadApiClient.ApiVersion2}/payloads/{PartitionKey}/{PayloadId}";
            var response = await _restfulApiAccessProvider.GetAsync(functionUrl).ConfigureAwait(false);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
            response.EnsureSuccessStatusCode();
            return true;
        }

        public async Task CreateNewAsync(string category = "Default", CancellationToken cancellationToken = default)
        {

            if (string.IsNullOrWhiteSpace(category))
            {
                throw new ArgumentException($"'{nameof(category)}' cannot be null or whitespace.", nameof(category));
            }

            var functionUrl = $"{_config.GetServiceBaseUri()}/api/{PayloadApiClient.ApiVersion2}/payloads/{PartitionKey}/{PayloadId}?category={category}&sign={_signProvider != null}";

            var response = await _restfulApiAccessProvider.PostJsonWithSignAsync(
                functionUrl,
                JsonConvert.SerializeObject(string.Empty),
                _signProvider,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            PayloadDefinitionDto payload;
            payload = await response.Content.ReadAsAsync<PayloadDefinitionDto>();
            if (payload == null)
            {
                throw new InvalidOperationException("Payload was not correctly created correctly. Returned null");
            }

            if (payload.Id != PayloadId)
            {
                throw new InvalidOperationException($"Payload was not correctly created correctly. Returned payload id:{payload.Id}, Expected payload id: {PayloadId}");
            }
        }

        public async Task DeleteAsync(CancellationToken cancellationToken = default)
        {
            var functionUrl = $"{_config.GetServiceBaseUri()}/api/{PayloadApiClient.ApiVersion2}/payloads/{PartitionKey}/{PayloadId}";
            var response = await _restfulApiAccessProvider.DeleteAsync(new Uri(functionUrl), cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }

        public IPayloadDataClient<TPayloadData> GetPayloadDataClient<TPayloadData>() where TPayloadData : class
        {
            return new PayloadDataClient<TPayloadData>(PartitionKey, PayloadId, _config, _restfulApiAccessProvider, _signProvider);
        }

        public IPayloadMetadataCollectionClient<TMetadataCollectionItem> GetPayloadMetadataCollectionClient<TMetadataCollectionItem>() where TMetadataCollectionItem : class
        {
            return new PayloadMetadataCollectionClient<TMetadataCollectionItem>(PartitionKey, PayloadId, _config, _restfulApiAccessProvider, _signProvider);
        }

        public IPayloadDataReferencesClient GetPayloadDataReferencesClient()
        {
            return new PayloadDataReferencesClient(PartitionKey, PayloadId, _config, _restfulApiAccessProvider, _signProvider);
        }
    }
}
