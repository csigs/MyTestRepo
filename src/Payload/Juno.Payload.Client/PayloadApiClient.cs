

namespace Juno.Payload.Client
{
    using System;
    using System.IO;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    using Juno.Payload.Client.Abstraction;
    using Juno.Payload.Client.Abstraction.V1;
    using Juno.Payload.Client.Configuration;
    using Juno.Payload.Dto;

    using Microsoft.Localization.DelegatingHandlers;
    using Microsoft.Localization.RestApiAccess;
    using Microsoft.Localization.SignProviders;
    using Juno.Payload.Client.Extensions;

    /// <summary>
    /// Provides functions for calling payload API.
    /// </summary>
    public class PayloadApiClient : IPayloadApiClient, IPayloadApiV2Client
    {
        internal const string ApiVersion1 = "v1";

        internal const string ApiVersion2 = "v2";

        public static readonly TimeSpan DefaultPayloadClientTimeOut = TimeSpan.FromMinutes(5); // 5 minutes default timeout
        /// <summary>
        /// Default partition key used in V1 api. If you would like to load v1 payload using v2 API you need to use this
        /// value as partition key.
        /// </summary>
        public const string DefaultPartitionKeyValue = "DefaultPartition";

        private readonly PayloadClientConfig _config;

        private readonly IRestfulApiAccessProvider _restfulApiAccessProvider;

        private readonly ISignProvider _signProvider;


        public PayloadApiClient(PayloadClientConfig config, ISignProvider signProvider = null)
            : this(config,
                  new RestfulApiAccessProvider(
                    new(new ExponentialBackoffRetryDelegatingHandler(
                        new BearerTokenDelegatingHandler(new[] { config.MSIScope }, new HttpClientHandler())
                        ))),
                  signProvider)
        {
        }

        public PayloadApiClient(PayloadClientConfig config, HttpClient httpClient, ISignProvider signProvider = null)
        : this(config, new RestfulApiAccessProvider(httpClient), signProvider)
        {
        }

        public PayloadApiClient(PayloadClientConfig config, IRestfulApiAccessProvider restfulApiAccessProvider, ISignProvider signProvider = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _restfulApiAccessProvider = restfulApiAccessProvider
                ?? throw new ArgumentNullException(nameof(restfulApiAccessProvider));
            _signProvider = signProvider;
        }

        //[Obsolete("Use V2 instead")]
        public async Task DeletePayloadAsync(Guid payloadId)
        {
            if (payloadId == Guid.Empty)
            {
                throw new ArgumentException("Value can't be empty", nameof(payloadId));
            }

            var functionUrl = $"{_config.GetServiceBaseUri()}/api/{ApiVersion1}/payloads/{payloadId}";
            var response = await _restfulApiAccessProvider.DeleteAsync(new Uri(functionUrl)).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }

        //[Obsolete("Use V2 instead")]
        public async Task DeletePayloadMetadataAsync(Guid payloadId)
        {
            if (payloadId == Guid.Empty)
            {
                throw new ArgumentException("Value can't be empty", nameof(payloadId));
            }

            var functionUrl = $"{_config.GetServiceBaseUri()}/api/{ApiVersion1}/payloads/{payloadId}/metadata";
            var response = await _restfulApiAccessProvider.DeleteAsync(new Uri(functionUrl)).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }

        //[Obsolete("Use V2 instead")]
        public IPayloadReadClient<TInlineMetadata, TMetadata> GetPayloadReadClient<TInlineMetadata, TMetadata>(Guid payloadId, Guid? fallbackBuildId = null)
            where TInlineMetadata : class
            where TMetadata : class
        {
            return new PayloadClient<TInlineMetadata, TMetadata>(payloadId, fallbackBuildId, _config, _restfulApiAccessProvider, _signProvider);
        }

        // [Obsolete("Use V2 instead")]
        public IPayloadClient<TInlineMetadata, TMetadata> GetPayloadClient<TInlineMetadata, TMetadata>(Guid payloadId, Guid? fallbackBuildId = null)
            where TInlineMetadata : class
            where TMetadata : class
        {
            if (payloadId == Guid.Empty)
            {
                throw new ArgumentException("Value can't be empty", nameof(payloadId));
            }

            return new PayloadClient<TInlineMetadata, TMetadata>(payloadId, fallbackBuildId, _config, _restfulApiAccessProvider, _signProvider);
        }

        public IPayloadClientV2 GetPayloadClient(Guid payloadId, string partitionKey)
        {
            if (payloadId == Guid.Empty)
            {
                throw new ArgumentException("Value can't be empty", nameof(payloadId));
            }

            return new PayloadClientV2(partitionKey, payloadId, _config, _restfulApiAccessProvider, _signProvider);
        }

        //[Obsolete("Use V2 instead")]
        public async Task<IPayloadClient<TInlineMetadata, TMetadata>> CreatePayloadAsync<TInlineMetadata, TMetadata>()
            where TInlineMetadata : class
            where TMetadata : class
        {
            var functionUrl = $"{_config.GetServiceBaseUri()}/api/{ApiVersion1}/payloads";
            var response = await _restfulApiAccessProvider.PostJsonAsync(functionUrl, "").ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            PayloadDefinitionDto payload;
            payload = await response.Content.ReadAsAsync<PayloadDefinitionDto>();
            if (payload == null || payload.Id == Guid.Empty)
            {
                throw new InvalidOperationException("Payload was not correctly created. Returned null or empty Id.");
            }

            return new PayloadClient<TInlineMetadata, TMetadata>(payload.Id, null, _config, _restfulApiAccessProvider, _signProvider);
        }

        //[Obsolete("Use V2 instead")]
        public async Task<IPayloadClient<TInlineMetadata, TMetadata>> CreatePayloadWithDataAsync<TInlineMetadata, TMetadata>(TInlineMetadata payloadInlineMetadata, string category = "Default", TimeSpan uploadTimeout = default)
            where TInlineMetadata : class
            where TMetadata : class
        {
            if (payloadInlineMetadata is null)
            {
                throw new ArgumentNullException(nameof(payloadInlineMetadata));
            }

            if (string.IsNullOrWhiteSpace(category))
            {
                throw new ArgumentException($"'{nameof(category)}' cannot be null or whitespace.", nameof(category));
            }

            if (uploadTimeout < TimeSpan.Zero)
            {
                throw new ArgumentException("Value can't be less than zero", nameof(uploadTimeout));
            }

            var functionUrl = $"{_config.GetServiceBaseUri()}/api/{ApiVersion1}/payloads?&withPayloadData=true&category={category}&payloadDataType=BlobAttachment&sign={_signProvider != null}";
            var response = await _restfulApiAccessProvider.PostJsonWithSignAsync(
                functionUrl,
                JsonConvert.SerializeObject(payloadInlineMetadata),
                _signProvider).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadAsAsync<PayloadDefinitionDto>();
            if (payload == null || payload.Id == Guid.Empty)
            {
                throw new InvalidOperationException("Payload was not correctly created. Returned null or empty Id.");
            }

            return new PayloadClient<TInlineMetadata, TMetadata>(payload.Id, null, _config, _restfulApiAccessProvider, _signProvider);
        }

        public async Task<IPayloadClientV2> CreatePayloadV2Async<TPayloadData>(string partitionKey, string category = "Default")
            where TPayloadData : class
        {

            if (string.IsNullOrWhiteSpace(partitionKey))
            {
                throw new ArgumentException($"'{nameof(partitionKey)}' cannot be null or whitespace.", nameof(partitionKey));
            }

            if (string.IsNullOrWhiteSpace(category))
            {
                throw new ArgumentException($"'{nameof(category)}' cannot be null or whitespace.", nameof(category));
            }


            var functionUrl = $"{_config.GetServiceBaseUri()}/api/{ApiVersion2}/payloads/{partitionKey}?&category={category}&sign={_signProvider != null}";
            var response = await _restfulApiAccessProvider.PostJsonAsync(functionUrl, JsonConvert.SerializeObject(string.Empty)).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            PayloadDefinitionDto payload;
            payload = await response.Content.ReadAsAsync<PayloadDefinitionDto>();
            if (payload == null || payload.Id == Guid.Empty)
            {
                throw new InvalidOperationException("Payload was not correctly created. Returned null or empty Id.");
            }

            return new PayloadClientV2(partitionKey, payload.Id, _config, _restfulApiAccessProvider, _signProvider);
        }

        public async Task<IPayloadClientV2> CreatePayloadWithDataV2Async<TPayloadData>(string partitionKey, TPayloadData payloadData, string category = "Default", PayloadDataStorageTypeDto payloadDataType = PayloadDataStorageTypeDto.BlobAttachment, TimeSpan uploadTimeout = default)
            where TPayloadData : class
        {

            if (string.IsNullOrWhiteSpace(partitionKey))
            {
                throw new ArgumentException($"'{nameof(partitionKey)}' cannot be null or whitespace.", nameof(partitionKey));
            }

            if (payloadData is null)
            {
                throw new ArgumentNullException(nameof(payloadData));
            }

            if (string.IsNullOrWhiteSpace(category))
            {
                throw new ArgumentException($"'{nameof(category)}' cannot be null or whitespace.", nameof(category));
            }

            if (uploadTimeout < TimeSpan.Zero)
            {
                throw new ArgumentException("Value can't be less than zero", nameof(uploadTimeout));
            }

            var functionUrl = $"{_config.GetServiceBaseUri()}/api/{ApiVersion2}/payloads/{partitionKey}?&withPayloadData=true&category={category}&payloadDataType={payloadDataType}&sign={_signProvider != null}";
            var response = await _restfulApiAccessProvider.PostJsonWithSignAsync(
                functionUrl,
                JsonConvert.SerializeObject(payloadData),
                _signProvider).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            PayloadDefinitionDto payload;
            payload = await response.Content.ReadAsAsync<PayloadDefinitionDto>();
            if (payload == null || payload.Id == Guid.Empty)
            {
                throw new InvalidOperationException("Payload was not correctly created. Returned null or empty Id.");
            }

            return new PayloadClientV2(partitionKey, payload.Id, _config, _restfulApiAccessProvider, _signProvider);
        }

        public async Task DeletePayloadAsync(Guid payloadId, string partitionKey)
        {
            var functionUrl = $"{_config.GetServiceBaseUri()}/api/{ApiVersion2}/payloads/{partitionKey}/{payloadId}";
            var response = await _restfulApiAccessProvider.DeleteAsync(new Uri(functionUrl)).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }

        public async Task DeletePayloadMetadataCollectionAsync(Guid payloadId, string partitionKey)
        {
            var functionUrl = $"{_config.GetServiceBaseUri()}/api/{ApiVersion1}/payloads/{payloadId}/metadata";
            var response = await _restfulApiAccessProvider.DeleteAsync(new Uri(functionUrl)).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }

        public async Task DeletePayloadDataReferencesAsync(Guid payloadId, string partitionKey)
        {
            var functionUrl = $"{_config.GetServiceBaseUri()}/api/{ApiVersion1}/payloads/{payloadId}/datarefs";
            var response = await _restfulApiAccessProvider.DeleteAsync(new Uri(functionUrl)).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }
    }
}
