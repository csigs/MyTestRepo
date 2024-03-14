namespace Juno.Payload.Client;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft;
using Newtonsoft.Json;

using Juno.Payload.Client.Abstraction;
using Juno.Payload.Client.Configuration;
using Juno.Payload.Client.Extensions;
using Juno.Payload.Contracts.Dto.Pagination;
using Microsoft.Localization.RestApiAccess;
using Microsoft.Localization.SignProviders;

internal class PayloadMetadataCollectionClient<TMetadataCollectionItem> : IPayloadMetadataCollectionClient<TMetadataCollectionItem> where TMetadataCollectionItem : class
{
    private readonly PayloadClientConfig _config;
    private readonly string _payloadIdUrl;
    private readonly IRestfulApiAccessProvider _restfulApiAccessProvider;
    private readonly ISignProvider _signProvider;

    public PayloadMetadataCollectionClient(
        string payloadPartitionKey,
        Guid payloadId,
        PayloadClientConfig config,
        IRestfulApiAccessProvider restfulApiAccessProvider,
        ISignProvider signProvider = null)
    {
        Requires.NotNullOrWhiteSpace(payloadPartitionKey, nameof(payloadPartitionKey));
        Requires.NotEmpty(payloadId, nameof(payloadId));
        Requires.NotNull(config, nameof(config));
        Requires.NotNull(restfulApiAccessProvider, nameof(restfulApiAccessProvider));

        PayloadPartitionKey = payloadPartitionKey;
        PayloadId = payloadId;
        _config = config;
        _restfulApiAccessProvider = restfulApiAccessProvider;

        _payloadIdUrl = $"{_config.GetServiceBaseUri()}/api/{PayloadApiClient.ApiVersion2}/payloads/{PayloadPartitionKey}/{PayloadId}";
        _signProvider = signProvider;
    }

    public Guid PayloadId { get; }

    public string PayloadPartitionKey { get; }

    public IAsyncEnumerable<TMetadataCollectionItem> ReadPayloadMetadataCollectionAsync(CancellationToken cancellationToken = default)
    {
        var functionUrl = GetFunctionUrl("metadata");

        return ReadAndEnumerateDataChunksAsync<TMetadataCollectionItem>(functionUrl, cancellationToken);
    }

    public async Task<IEnumerable<TMetadataCollectionItem>> ReadPayloadMetadataCollectionForAsync(IEnumerable<Guid> metadataIds, CancellationToken cancellationToken = default)
    {

        var functionUrl = GetFunctionUrl("MetadataById");

        var result = await _restfulApiAccessProvider.PostJsonWithSignAsync(
            functionUrl,
            new { MetadataIds = metadataIds },
            _signProvider,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        result.EnsureSuccessStatusCode();

        var responseContent = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
        result.VerifySignature(_signProvider, responseContent);
        return JsonConvert.DeserializeObject<IEnumerable<TMetadataCollectionItem>>(responseContent);
    }

    public async Task AddPayloadMetadataCollectionItemsAsync(
        IEnumerable<TMetadataCollectionItem> metadata,
        bool useTransaction = true,
        CancellationToken cancellationToken = default)
    {
        Requires.NotNullEmptyOrNullElements(metadata, nameof(metadata));

        var functionUrl = GetFunctionUrl("metadata");

        var result = await _restfulApiAccessProvider.PostJsonWithSignAsync(
            functionUrl,
            metadata,
            _signProvider,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        result.EnsureSuccessStatusCode();
    }

    public async Task UpdatePayloadMetadataCollectionItemsAsync(IEnumerable<TMetadataCollectionItem> metadata, CancellationToken cancellationToken = default)
    {
        Requires.NotNullEmptyOrNullElements(metadata, nameof(metadata));

        var functionUrl = GetFunctionUrl("metadata");

        var result = await _restfulApiAccessProvider.PostJsonWithSignAsync(
            functionUrl,
            metadata,
            _signProvider,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        result.EnsureSuccessStatusCode();
    }

    public async Task DeleteMetadataCollectionAsync(CancellationToken cancellationToken = default)
    {
        var functionUrl = GetFunctionUrl("metadata");

        var result = await _restfulApiAccessProvider.DeleteAsync(
            new Uri(functionUrl),
            cancellationToken).ConfigureAwait(false);

        result.EnsureSuccessStatusCode();
    }

    public async IAsyncEnumerable<TDataCollectionItem> ReadAndEnumerateDataChunksAsync<TDataCollectionItem>(string requestUri, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Requires.NotNullOrWhiteSpace(requestUri, nameof(requestUri));

        string continuationToken = null;
        do
        {
            var result = await _restfulApiAccessProvider.GetAsync(AttachContinuationTokenIfNeeded(requestUri, continuationToken), cancellationToken).ConfigureAwait(false); // this retry will be handled on top level through HttpClient using TransientHttpErrorPolicy through Http.Polly extension
            result.EnsureSuccessStatusCode();

            var returnedDataChunk = await result.ReadAsJsonAsync<DataChunkDto<TDataCollectionItem>>(_signProvider).ConfigureAwait(false);
            Verify.Operation(returnedDataChunk != null, "Returned metadata collection is null");

            foreach (var item in returnedDataChunk.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return item;
            }
            continuationToken = returnedDataChunk.ContinuationToken?.Replace("\\\"", "\""); // continuation token arrives as json in string. So it's escaped in style [{\"Property1\",\"value1\"}] instead of [{"Property1","value1"}] 
        }
        while (continuationToken != null);
    }

    private string AttachContinuationTokenIfNeeded(string callingUri, string continuationToken = null)
    {
        if (string.IsNullOrWhiteSpace(callingUri))
        {
            throw new ArgumentException($"'{nameof(callingUri)}' cannot be null or whitespace.", nameof(callingUri));
        }

        if (!string.IsNullOrWhiteSpace(continuationToken))
        {
            return string.Format($"{callingUri}{(callingUri.Contains('?') ? '&' : '?')}continuationToken={Uri.EscapeDataString(continuationToken)}");
        }
        return callingUri;
    }

    private string GetFunctionUrl(string pathToAppend = default, bool useTransaction = false)
    {
        var functionUrl = _payloadIdUrl;

        if (pathToAppend is not null)
        {
            functionUrl += "/" + pathToAppend;
        }

        if (useTransaction)
        {
            functionUrl += "?useTransaction=true";
        }

        if (_signProvider != null)
        {
            functionUrl += $"{(functionUrl.Contains('?') ? '&' : '?')}sign=true";
        }

        return functionUrl;
    }
}
