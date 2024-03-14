namespace Juno.Payload.Client
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Primitives;
    using Newtonsoft.Json;

    using Juno.Payload.Client.Abstraction;
    using Juno.Payload.Client.Configuration;
    using Juno.Payload.Client.Extensions;
    using Juno.Payload.Dto;
    using Microsoft.Localization.RestApiAccess;
    using Microsoft.Localization.SignProviders;

    internal class PayloadDataClient<TPayloadData> : IPayloadDataClient<TPayloadData> where TPayloadData : class
    {
        private readonly PayloadClientConfig _config;
        private readonly IRestfulApiAccessProvider _restfulApiAccessProvider;
        private readonly ISignProvider _signProvider;

        public PayloadDataClient(string payloadPartitionKey, Guid payloadId, PayloadClientConfig config, IRestfulApiAccessProvider restfulApiAccessProvider, ISignProvider signProvider = null)
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

        public async Task<TPayloadData> ReadPayloadDataAsync(CancellationToken cancellationToken = default)
        {
            var functionUrl = $"{_config.GetServiceBaseUri()}/api/{PayloadApiClient.ApiVersion2}/payloads/{PayloadPartitionKey}/{PayloadId}/data?sign={_signProvider != null}";
            var response = await _restfulApiAccessProvider.GetAsync(functionUrl, cancellationToken).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentEncoding != null && response.Content.Headers.ContentEncoding.Contains("gzip"))
            {
                var responseCompressedBytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

                response.VerifySignature(_signProvider, responseCompressedBytes);

                var responseDecompressedBytes = await DecompressBytesAsync(responseCompressedBytes);

                if (typeof(TPayloadData) == typeof(Stream))
                {
                    var outputStream = new MemoryStream(responseDecompressedBytes);
                    return outputStream as TPayloadData;
                }
                else
                {
                    return JsonConvert.DeserializeObject<TPayloadData>(Encoding.UTF8.GetString(responseDecompressedBytes));
                }
            }
            else
            {
                if (typeof(TPayloadData) == typeof(Stream))
                {
                    var responseBytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

                    response.VerifySignature(_signProvider, responseBytes);

                    var outputStream = new MemoryStream(responseBytes);
                    await response.Content.CopyToAsync(outputStream);
                    await outputStream.FlushAsync();
                    outputStream.Seek(0, SeekOrigin.Begin);
                    return outputStream as TPayloadData;
                }
                else
                {
                    return await response.ReadAsJsonAsync<TPayloadData>(_signProvider).ConfigureAwait(false);
                }
            }
        }

        public async Task UploadPayloadDataAsync(TPayloadData payloadData, PayloadDataStorageTypeDto payloadDataType = PayloadDataStorageTypeDto.BlobAttachment, TimeSpan uploadTimeout = default, CancellationToken cancellationToken = default)
        {
            var functionUrl = $"{_config.GetServiceBaseUri()}/api/{PayloadApiClient.ApiVersion2}/payloads/{PayloadPartitionKey}/{PayloadId}/data?&payloadDataType={payloadDataType}&sign={_signProvider != null}";

            var payloadSourceStream = payloadData is Stream ? payloadData as Stream : new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payloadData)));

            var inputCompressedData = await CompressBytesAsync(payloadSourceStream.ToArray());

            using (var httpRequest = new HttpRequestMessage(HttpMethod.Put, new Uri(functionUrl)))
            {
                var dataToSend = new MemoryStream(inputCompressedData);
                using (var streamContent = new StreamContent(dataToSend))
                {
                    //TODO: figure out how to apply timeout
                    streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                    streamContent.Headers.Add("Content-Encoding", "gzip");
                    httpRequest.AddSignatureHeader(_signProvider, inputCompressedData);
                    httpRequest.Content = streamContent;

                    var headers = new Dictionary<string, StringValues>();
                    if (_signProvider != null)
                    {
                       var signature = _signProvider.Sign(inputCompressedData);
                        headers.Add(Constants.SignatureHttpHeader, signature);
                    }

                    var result = await _restfulApiAccessProvider.PutAsync(new Uri(functionUrl), httpRequest.Content, headers, cancellationToken: cancellationToken).ConfigureAwait(false); //need to test this it was sendAsync
                    result.EnsureSuccessStatusCode();
                }
            }
        }

        private static async Task<byte[]> CompressBytesAsync(byte[] bytes, CancellationToken cancel = default(CancellationToken))
        {
            using (var outputStream = new MemoryStream())
            {
                using (var compressionStream = new GZipStream(outputStream, CompressionLevel.Optimal))
                {
                    await compressionStream.WriteAsync(bytes, 0, bytes.Length, cancel);
                }
                return outputStream.ToArray();
            }
        }

        private static async Task<byte[]> DecompressBytesAsync(byte[] bytes, CancellationToken cancel = default(CancellationToken))
        {
            using (var inputStream = new MemoryStream(bytes))
            {
                using (var outputStream = new MemoryStream())
                {
                    using (var compressionStream = new GZipStream(inputStream, CompressionMode.Decompress))
                    {
                        await compressionStream.CopyToAsync(outputStream, 4096, cancel);
                    }
                    return outputStream.ToArray();
                }
            }
        }
    }
}
