namespace Juno.Payload.Client
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    using Juno.Common.DataReference;
    using Juno.Common.Metadata;
    using Juno.Payload.Client.Abstraction;
    using Juno.Payload.Client.Configuration;
    using Juno.Payload.Client.Extensions;
    using Juno.Payload.Contracts.Dto.Pagination;
    using Microsoft.Extensions.Primitives;
    using Microsoft.Localization.RestApiAccess;
    using Microsoft.Localization.SignProviders;

    using Newtonsoft.Json;

    internal class PayloadDataReferencesClient : IPayloadDataReferencesClient
    {
        private readonly PayloadClientConfig _config;
        private readonly IRestfulApiAccessProvider _restfulApiAccessProvider;
        private readonly ISignProvider _signProvider;

        public PayloadDataReferencesClient(string payloadPartitionKey, Guid payloadId, PayloadClientConfig config, IRestfulApiAccessProvider restfulApiAccessProvider, ISignProvider signProvider = null)
        {
            if (string.IsNullOrWhiteSpace(payloadPartitionKey))
            {
                throw new ArgumentException($"'{nameof(payloadPartitionKey)}' cannot be null or whitespace.", nameof(payloadPartitionKey));
            }

            if (payloadId == Guid.Empty)
            {
                throw new ArgumentException(nameof(payloadId), "Value can't be empty");
            }

            PayloadPartitionKey = payloadPartitionKey;
            PayloadId = payloadId;
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _restfulApiAccessProvider = restfulApiAccessProvider
                ?? throw new ArgumentNullException(nameof(restfulApiAccessProvider));
            _signProvider = signProvider;
        }

        public string PayloadPartitionKey { get; }

        public Guid PayloadId { get; }

        public IAsyncEnumerable<LocElementDataReferenceDescriptor> ReadStoredLocElementDataReferencesAsync(CancellationToken cancellationToken = default)
        {
            if (PayloadId == default)
            {
                throw new ArgumentNullException("Payload Id is not set yet.");
            }

            var functionUrl = $"{_config.GetServiceBaseUri()}/api/{PayloadApiClient.ApiVersion2}/payloads/{PayloadPartitionKey}/{PayloadId}/datarefs?sign={_signProvider != null}";

            return ReadAndEnumerateDataChunksAsync<LocElementDataReferenceDescriptor>(functionUrl, cancellationToken);
        }

        public async Task<IEnumerable<LocElementDataReferenceDescriptor>> ReadStoredLocElementDataReferencesChunkForAsync(IEnumerable<ILocElementMetadata> locElementMetadata, CancellationToken cancellationToken = default)
        {
            if (locElementMetadata == null)
            {
                throw new ArgumentNullException(nameof(locElementMetadata));
            }

            if (PayloadId == default)
            {
                throw new ArgumentNullException("Payload Id is not set yet.");
            }

            return await ReadStoredLocElementDataReferencesChunkForAsync(locElementMetadata.Select(i => i.Id), cancellationToken);
        }

        public async Task<IEnumerable<LocElementDataReferenceDescriptor>> ReadStoredLocElementDataReferencesChunkForAsync(IEnumerable<Guid> locElementMetadataIds, CancellationToken cancellationToken = default)
        {
            if (PayloadId == default)
            {
                throw new ArgumentNullException("Payload Id is not set yet.");
            }

            var functionUrl = $"{_config.GetServiceBaseUri()}/api/{PayloadApiClient.ApiVersion1}/payloads/{PayloadId}/datarefs/GetByProvidedIds?sign={_signProvider != null}";

            var result = await _restfulApiAccessProvider.PostJsonWithSignAsync(
                functionUrl,
                new
                {
                    ProvidedIds = locElementMetadataIds
                },
                _signProvider,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            result.EnsureSuccessStatusCode();

            var responseContent = await result.Content.ReadAsStringAsync().ConfigureAwait(false);

            result.VerifySignature(_signProvider, responseContent);

            return JsonConvert.DeserializeObject<IEnumerable<LocElementDataReferenceDescriptor>>(responseContent);
        }

        public async Task AddLocElementDataReferencesAsync(IEnumerable<LocElementDataReferenceDescriptor> dataElements, bool useTransaction = true, CancellationToken cancellationToken = default)
        {
            if (PayloadId == default)
            {
                throw new ArgumentNullException("Payload Id is not set yet.");
            }

            var functionUrl = $"{_config.GetServiceBaseUri()}/api/{PayloadApiClient.ApiVersion2}/payloads/{PayloadPartitionKey}/{PayloadId}/datarefs?sign={_signProvider != null}";
            if (useTransaction)
            {
                functionUrl = functionUrl + "&useTransaction=true";
            }

            var result = await _restfulApiAccessProvider.PostJsonWithSignAsync(
                functionUrl,
                new { DataReferences = dataElements },
                _signProvider,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            result.EnsureSuccessStatusCode();
        }

        public async Task DeleteDataReferencesAsync(CancellationToken cancellationToken = default)
        {
            if (PayloadId == default)
            {
                throw new ArgumentNullException("Payload Id is not set yet.");
            }

            var functionUrl = $"{_config.GetServiceBaseUri()}/api/{PayloadApiClient.ApiVersion2}/payloads/{PayloadPartitionKey}/{PayloadId}/datarefs";

            var result = await _restfulApiAccessProvider.DeleteAsync(
                new Uri(functionUrl),
                cancellationToken).ConfigureAwait(false);

            result.EnsureSuccessStatusCode();
        }
        private static string AttachContinuationTokenIfNeeded(string callingUri, string continuationToken = null)
        {
            if (string.IsNullOrWhiteSpace(callingUri))
            {
                throw new ArgumentException($"'{nameof(callingUri)}' cannot be null or whitespace.", nameof(callingUri));
            }

            if (!string.IsNullOrWhiteSpace(continuationToken))
            {
                return string.Format($"{callingUri}&continuationToken={Uri.EscapeDataString(continuationToken)}");
            }
            return callingUri;
        }

        public async IAsyncEnumerable<TDataCollectionItem> ReadAndEnumerateDataChunksAsync<TDataCollectionItem>(string requestUri, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(requestUri))
            {
                throw new ArgumentException($"'{nameof(requestUri)}' cannot be null or whitespace.", nameof(requestUri));
            }

            string continuationToken = null;
            do
            {
                var result = await _restfulApiAccessProvider.GetAsync(AttachContinuationTokenIfNeeded(requestUri, continuationToken), cancellationToken).ConfigureAwait(false); // this retry will be handled on top level through HttpClient using TransientHttpErrorPolicy through Http.Polly extension
                result.EnsureSuccessStatusCode();
                var returnedDataChunk = await result.ReadAsJsonAsync<DataChunkDto<TDataCollectionItem>>(_signProvider).ConfigureAwait(false);
                if (returnedDataChunk == null)
                {
                    throw new InvalidOperationException("Returned metadata collection is null");
                }
                foreach (var item in returnedDataChunk.Items)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return item;
                }
                continuationToken = returnedDataChunk.ContinuationToken?.Replace("\\\"", "\""); // continuation token arrives as json in string. So it's escaped in style [{\"Property1\",\"value1\"}] instead of [{"Property1","value1"}] 
            }
            while (continuationToken != null);
        }
    }
}
