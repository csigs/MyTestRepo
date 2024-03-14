namespace Juno.Payload.Client.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Runtime.CompilerServices;
    using System.Threading;

    using Juno.Payload.Contracts.Dto.Pagination;
    using Microsoft.Localization.SignProviders;

    internal static class HttpClientExtensions
    {
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

        public static async IAsyncEnumerable<TDataCollectionItem> ReadAndEnumerateDataChunksAsync<TDataCollectionItem>(
            this HttpClient httpClient,
            string requestUri,
            ISignProvider signProvider,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (httpClient is null)
            {
                throw new ArgumentNullException(nameof(httpClient));
            }

            if (string.IsNullOrWhiteSpace(requestUri))
            {
                throw new ArgumentException($"'{nameof(requestUri)}' cannot be null or whitespace.", nameof(requestUri));
            }


            string continuationToken = null;
            do
            {
                var result = await httpClient.GetAsync(AttachContinuationTokenIfNeeded(requestUri, continuationToken), cancellationToken).ConfigureAwait(false); // this retry will be handled on top level through HttpClient using TransientHttpErrorPolicy through Http.Polly extension
                result.EnsureSuccessStatusCode();
                var returnedDataChunk = await result.ReadAsJsonAsync<DataChunkDto<TDataCollectionItem>>(signProvider).ConfigureAwait(false);
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
