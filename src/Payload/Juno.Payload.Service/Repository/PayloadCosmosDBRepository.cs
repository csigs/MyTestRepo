// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DocumentDBRepository.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//   Provides functions to handle document db
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Juno.Payload.Service.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Net;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure;
    using Juno.Payload.Service.Model;
    using Microsoft.Azure.Cosmos;
    using Microsoft.Azure.Cosmos.Linq;
    using Microsoft.Localization.Juno.Common.Data.Repository;
    using Polly;


    /// <summary>
    /// Provides functions to access payload store document db.
    /// </summary>
    public class PayloadCosmosDBRepository :
        CosmosDBRepository,
        IPayloadRepository,
        IPayloadMetadataRepository,
        IPayloadDataRefRepository
    {
        private const int RetryWaitingTimeBaseInSec = 7;

        private const int AddWaitOnTopTooManyRequestInMs = 500;

        /// <summary>
        /// Document DB client.
        /// </summary>
        private readonly CosmosClient _client;

        /// <summary>
        /// Collection Id
        /// </summary>
        private readonly string _containerName;

        /// <summary>
        /// Database Id
        /// </summary>
        private readonly string _databaseName;

        /// <summary>
        /// Construct DocumentDBRepository instance.
        /// </summary>
        /// <param name="documentClient">The DocumentClient instance.</param>
        /// <param name="databaseName">Database Id.</param>
        /// <param name="containerName">container name (former Collection Id from document db v2 api).</param>
        /// <param name="partitionKeyPath">Optional partition key path for partitioning collection.</Optional></param>
        internal PayloadCosmosDBRepository(CosmosClient documentClient, string databaseName, string containerName, string partitionKeyPath = null)
            : base(documentClient, databaseName, containerName, partitionKeyPath)
        {
            _client = documentClient ?? throw new ArgumentNullException(nameof(documentClient));
            _databaseName = databaseName ?? throw new ArgumentNullException(nameof(databaseName));
            _containerName = containerName ?? throw new ArgumentNullException(nameof(containerName));
        }

        private bool ShouldRetry(HttpStatusCode httpStatusCode)
        {
            return httpStatusCode == HttpStatusCode.TooManyRequests || httpStatusCode == HttpStatusCode.ServiceUnavailable || httpStatusCode == HttpStatusCode.BadGateway || httpStatusCode == HttpStatusCode.RequestTimeout;
        }

        /// <summary>
        /// Gets the list of items that are in <paramref name="ids"/>.
        /// </summary>
        /// <param name="ids">
        /// List of Ids to search.
        /// </param>
        /// <returns>
        /// List of items that matches the query.
        /// </returns>
        public new async Task<IEnumerable<T>> GetItemsAsync<T>(IEnumerable<Guid> ids) where T : class
        {
            var querySpec = new QueryDefinition($"select * from c where c.id IN @ids").WithParameter("ids", ids.ToArray());
            using var queryIterator = _client.GetContainer(_databaseName, _containerName).GetItemQueryIterator<T>(
                querySpec,
                requestOptions: new QueryRequestOptions());

            var results = new List<T>();

            while (queryIterator.HasMoreResults)
            {
                var response = await queryIterator.ReadNextAsync();
                results.AddRange(response.Resource);
            }

            return results;
        }

        /// <summary>
        /// Upserts item in cosmos db container
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <returns></returns>
        public async Task<bool> UpsertItemAsync<T>(T item) where T : class
        {
            var result = await _client.GetContainer(_databaseName, _containerName).UpsertItemAsync(item);

            return result.StatusCode == HttpStatusCode.OK || result.StatusCode == HttpStatusCode.Created;
        }

        public new async Task<bool> UpsertItemsAsync<T>(IEnumerable<T> items) where T : class
        {
            var responses = new List<HttpStatusCode>();
            var container = _client.GetContainer(_databaseName, _containerName);
            foreach (var item in items)
            {
                if (item is IIdentifiableObject)
                {
                    var id = ((IIdentifiableObject)item).Id;
                    var result = await container.UpsertItemAsync(item);
                    responses.Add(result.StatusCode);
                }
            }

            return !responses.Any(status => status == HttpStatusCode.OK || status == HttpStatusCode.Created) ? false : true;
        }

        public async Task<DataChunk<T>> GetItemsChunkAsync<T>(Expression<Func<T, bool>> predicate, string partitionKey, string continuationToken = null, CancellationToken cancellationToken = default) where T : class
        {
            if (predicate is null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            if (string.IsNullOrWhiteSpace(continuationToken))
            {
                continuationToken = null;
            }

            var result = new List<T>();
            var collectionQueryable = _client.GetContainer(_databaseName, _containerName).GetItemLinqQueryable<T>(continuationToken: continuationToken, requestOptions: new QueryRequestOptions() { PartitionKey = new PartitionKey(partitionKey), MaxItemCount = 1000 });
            var feedIterator = collectionQueryable.Where(predicate).ToFeedIterator();

            var hasMoreResults = feedIterator.HasMoreResults;
            if (hasMoreResults)
            {
                FeedResponse<T> feedResponse = await feedIterator.ReadNextAsync(cancellationToken);
                foreach (var item in feedResponse)
                {
                    result.Add(item);
                }
                return new DataChunk<T>(feedResponse.ContinuationToken, result);
            }
            else
            {
                return new DataChunk<T>(null, result);
            }
        }

        public async Task<IEnumerable<T>> GetItemsAsync<T>(Expression<Func<T, bool>> predicate, string partitionKey, CancellationToken cancellationToken = default) where T : class
        {
            if (predicate is null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }
            var result = new List<T>();
            var collectionQueryable = _client.GetContainer(_databaseName, _containerName).GetItemLinqQueryable<T>(requestOptions: new QueryRequestOptions() { PartitionKey = new PartitionKey(partitionKey), MaxItemCount = 1000 });
            var feedIterator = collectionQueryable.Where(predicate).ToFeedIterator();

            while (feedIterator.HasMoreResults)
            {
                foreach (var item in await feedIterator.ReadNextAsync(cancellationToken))
                {
                    result.Add(item);
                }
            }
            return result;
        }

        public async Task DeleteItemsSteamAsync<T>(Expression<Func<T, bool>> predicate, string partitionKey, CancellationToken cancellationToken = default) where T : class, IIdentifiableObject
        {
            if (predicate is null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            var container = _client.GetContainer(_databaseName, _containerName);

            DataChunk<T> chunk = null;
            do
            {
                chunk = await GetItemsChunkAsync<T>(predicate, partitionKey, chunk?.ContinuationToken, cancellationToken);

                foreach (var item in chunk.Items)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await container.DeleteItemAsync<T>(item.Id, new PartitionKey(partitionKey));
                }
                cancellationToken.ThrowIfCancellationRequested();
            }
            while (chunk.ContinuationToken != null);
        }

        public async Task CreateItemsBatchAsync<T>(IEnumerable<T> items, string partitionKey, CancellationToken cancellationToken = default) where T : class, IIdentifiableObject
        {
            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            if (string.IsNullOrWhiteSpace(partitionKey))
            {
                throw new ArgumentException($"'{nameof(partitionKey)}' cannot be null or whitespace.", nameof(partitionKey));
            }

            var retryPolicy = Policy
                 .HandleResult<TransactionalBatchResponse>(response => ShouldRetry(response.StatusCode))
                 .WaitAndRetryAsync(3,
                    sleepDurationProvider: (retryCount, status, ctx) => { return status.Result?.RetryAfter ?? TimeSpan.FromSeconds(RetryWaitingTimeBaseInSec * retryCount) + TimeSpan.FromMilliseconds(AddWaitOnTopTooManyRequestInMs); },
                     onRetryAsync:  (status, timeSpan, retryCount, ctx) =>
                     {
                         Trace.TraceWarning($"Retrying after status code {status.Result?.StatusCode} after {timeSpan.TotalMilliseconds} ms, retry attempt no #{retryCount}, partition key:{partitionKey}");
                         return Task.CompletedTask;
                     });

            var result = await retryPolicy.ExecuteAsync(async () =>
            {

                var batch = _client.GetContainer(_databaseName, _containerName).CreateTransactionalBatch(new PartitionKey(partitionKey));
                foreach (var item in items)
                {
                    batch.CreateItem(item);
                }
                var response = await batch.ExecuteAsync(cancellationToken);
                return response;
            }
            );

            if(!result.IsSuccessStatusCode)
            {
                
                throw new Exception($"Transactinal batch failed to create items, StatusCode: {result.StatusCode}, ErrorMessage:{result.ErrorMessage}");
            }
        }

    }
}
