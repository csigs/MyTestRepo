namespace Juno.Payload.Client
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft;
    using Newtonsoft.Json;

    using Juno.Common.DataReference;
    using Juno.Common.Metadata;
    using Juno.Payload.Client.Abstraction.V1;
    using Juno.Payload.Client.Configuration;
    using Juno.Payload.Client.Extensions;
    using Microsoft.Localization.SignProviders;
    using Microsoft.Localization.RestApiAccess;

    //[Obsolete("Use client V2 instead.")] commented uncommenting to avoid SDL bugs.
    internal class PayloadClient<TPayloadInlineMetadata, TMetadata> : IPayloadClient<TPayloadInlineMetadata, TMetadata>
        where TPayloadInlineMetadata : class
        where TMetadata : class
    {
        private static readonly TimeSpan DEFAULT_HTTP_TIMEOUT = TimeSpan.FromMinutes(5);

        private readonly PayloadClientConfig _config;
        private readonly ISignProvider _signProvider;

        private readonly string _payloadIdUrl;

        private readonly IRestfulApiAccessProvider _restfulApiAccessProvider;

        public PayloadClient(Guid payloadId, Guid? fallbackBuildId, PayloadClientConfig config, IRestfulApiAccessProvider restfulApiAccessProvider, ISignProvider signProvider = null)
        {
            Requires.NotEmpty(payloadId, "Specify a valid payload ID.");
            Requires.NotNull(config, nameof(config));
            Requires.NotNull(restfulApiAccessProvider, nameof(restfulApiAccessProvider));

            PayloadId = payloadId;
            FallbackBuildId = fallbackBuildId;
            _config = config;
            _restfulApiAccessProvider = restfulApiAccessProvider;

            _payloadIdUrl = $"{_config.GetServiceBaseUri()}/api/{PayloadApiClient.ApiVersion1}/payloads/{payloadId}";
            _signProvider = signProvider;
        }

        public Guid PayloadId { get; }

        [Obsolete("No longer used?")]
        public Guid? FallbackBuildId { get; }

        public PayloadClient(Guid payloadId, PayloadClientConfig config, IRestfulApiAccessProvider restfulApiAccessProvider, ISignProvider signProvider = null)
            : this(payloadId, null, config, restfulApiAccessProvider, signProvider)
        {
        }

        public async Task<bool> ExistsAsync(CancellationToken cancellationToken)
        {
            var client = _restfulApiAccessProvider.HttpClient;
            var functionUrl = GetFunctionUrl();
            var response = await client.GetAsync(functionUrl, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }

            response.EnsureSuccessStatusCode();

            return true;
        }

        public async Task<TPayloadInlineMetadata> GetInlineMetadataAsync(CancellationToken cancellationToken)
        {
            var client = _restfulApiAccessProvider.HttpClient;
            var functionUrl = GetFunctionUrl("inlinemetadata");
            var response = await client.GetAsync(functionUrl, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var metadata = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            response.VerifySignature(_signProvider, metadata);
            return JsonConvert.DeserializeObject<TPayloadInlineMetadata>(metadata);
        }

        public async Task UpdatePayloadInlineMetadataAsync(TPayloadInlineMetadata payloadMetadata, CancellationToken cancellationToken)
        {
            var client = _restfulApiAccessProvider.HttpClient;
            var functionUrl = GetFunctionUrl("inlinemetadata");

            var data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payloadMetadata));
            using var httpRequest = new HttpRequestMessage(HttpMethod.Put, new Uri(functionUrl));
            using var memoryStream = new MemoryStream(data);
            using var streamContent = new StreamContent(memoryStream, 4096);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            httpRequest.AddSignatureHeader(_signProvider, data);
            httpRequest.Content = streamContent;

            var result = await client.SendAsync(httpRequest, cancellationToken);
            result.EnsureSuccessStatusCode();
        }

        public async Task<IEnumerable<LocElementDataReferenceDescriptor>> GetStoredLocElementDataReferencesAsync(CancellationToken cancellationToken)
        {
            var client = _restfulApiAccessProvider.HttpClient;
            var functionUrl = GetFunctionUrl("datarefs");

            var result = await client.GetAsync(functionUrl, cancellationToken);
            result.EnsureSuccessStatusCode();

            var responseContent = await result.Content.ReadAsStringAsync();
            result.VerifySignature(_signProvider, responseContent);
            return JsonConvert.DeserializeObject<IEnumerable<LocElementDataReferenceDescriptor>>(responseContent);
        }

        public async Task<IEnumerable<LocElementDataReferenceDescriptor>> GetStoredLocElementDataReferencesAsync(
            IEnumerable<ILocElementMetadata> locElementMetadata,
            CancellationToken cancellationToken)
        {
            Requires.NotNullEmptyOrNullElements(locElementMetadata, nameof(locElementMetadata));

            return await GetStoredLocElementDataReferencesAsync(locElementMetadata.Select(i => i.Id), cancellationToken);
        }

        public async Task<IEnumerable<LocElementDataReferenceDescriptor>> GetStoredLocElementDataReferencesAsync(
            IEnumerable<Guid> locElementIds,
            CancellationToken cancellationToken)
        {
            Requires.NotNullOrEmpty(locElementIds, nameof(locElementIds));

            var client = _restfulApiAccessProvider.HttpClient;
            var functionUrl = GetFunctionUrl("datarefs/GetByProvidedIds");

            using var request = new { ProvidedIds = locElementIds }
                .ToHttpRequestMessageWithJsonContent(HttpMethod.Post, functionUrl, _signProvider);
            var result = await client.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);
            result.EnsureSuccessStatusCode();

            var responseContent = await result.Content.ReadAsStringAsync();

            result.VerifySignature(_signProvider, responseContent);

            return JsonConvert.DeserializeObject<IEnumerable<LocElementDataReferenceDescriptor>>(responseContent);
        }

        public async Task<IEnumerable<LocElementDataReferenceDescriptor>> GetLocElementDataReferencesAsync(
            IEnumerable<ILocElementMetadata> locElements,
            CancellationToken cancellationToken)
        {
            Requires.NotNullEmptyOrNullElements(locElements, nameof(locElements));

            return await GetLocElementDataReferencesAsync(locElements.Select(i => i.Id), cancellationToken);
        }

        public async Task<IEnumerable<LocElementDataReferenceDescriptor>> GetLocElementDataReferencesAsync(
            IEnumerable<Guid> locElementIds,
            CancellationToken cancellationToken)
        {
            Requires.NotNullOrEmpty(locElementIds, nameof(locElementIds));

            var client = _restfulApiAccessProvider.HttpClient;
            var functionUrl = GetFunctionUrl("datarefs/GetByProvidedIds");

            using var request = new { ProvidedIds = locElementIds }
                .ToHttpRequestMessageWithJsonContent(HttpMethod.Post, functionUrl, _signProvider);
            var result = await client.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);
            result.EnsureSuccessStatusCode();

            var responseContent = await result.Content.ReadAsStringAsync();
            result.VerifySignature(_signProvider, responseContent);
            return JsonConvert.DeserializeObject<IEnumerable<LocElementDataReferenceDescriptor>>(responseContent);
        }

        public async Task UploadDataReferencesAsync(
            IEnumerable<LocElementDataReferenceDescriptor> dataElements,
            bool useTransaction = false,
            CancellationToken cancellationToken = default)
        {
            var client = _restfulApiAccessProvider.HttpClient;
            var functionUrl = GetFunctionUrl("datarefs", useTransaction);

            using var request = new { DataReferences = dataElements }
                .ToHttpRequestMessageWithJsonContent(HttpMethod.Post, functionUrl, _signProvider);
            var result = await client.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);
            result.EnsureSuccessStatusCode();
        }

        public async Task<IEnumerable<TMetadata>> GetPayloadMetadataAsync(CancellationToken cancellationToken)
        {
            var client = _restfulApiAccessProvider.HttpClient;
            var functionUrl = GetFunctionUrl("metadata");

            var result = await client.GetAsync(functionUrl, cancellationToken);
            result.EnsureSuccessStatusCode();

            var responseContent = await result.Content.ReadAsStringAsync();
            result.VerifySignature(_signProvider, responseContent);
            return JsonConvert.DeserializeObject<IEnumerable<TMetadata>>(responseContent);
        }

        public async Task<IEnumerable<TMetadata>> GetPayloadMetadataAsync(List<Guid> providedIds, CancellationToken cancellationToken)
        {
            var client = _restfulApiAccessProvider.HttpClient;
            var functionUrl = GetFunctionUrl("MetadataById");

            using var request = new { ProvidedIds = providedIds }
                .ToHttpRequestMessageWithJsonContent(HttpMethod.Post, functionUrl, _signProvider);
            var result = await client.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);
            result.EnsureSuccessStatusCode();

            var responseContent = await result.Content.ReadAsStringAsync();
            result.VerifySignature(_signProvider, responseContent);
            return JsonConvert.DeserializeObject<IEnumerable<TMetadata>>(responseContent);
        }

        public async Task UploadPayloadMetadataAsync(
            IEnumerable<TMetadata> metadata,
            bool useTransaction = false,
            CancellationToken cancellationToken = default)
        {
            var client = _restfulApiAccessProvider.HttpClient;
            var functionUrl = GetFunctionUrl("metadata", useTransaction);

            using var request = metadata
                .ToHttpRequestMessageWithJsonContent(HttpMethod.Post, functionUrl, _signProvider);
            var result = await client.SendAsync(request, cancellationToken)
                            .ConfigureAwait(false);
            result.EnsureSuccessStatusCode();
        }

        public async Task UpdatePayloadMetadataAsync(IEnumerable<TMetadata> metadata, CancellationToken cancellationToken)
        {
            var client = _restfulApiAccessProvider.HttpClient;
            var functionUrl = GetFunctionUrl("metadata");

            using var request = metadata
                .ToHttpRequestMessageWithJsonContent(HttpMethod.Post, functionUrl, _signProvider);
            var result = await client.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);

            result.EnsureSuccessStatusCode();
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
}
