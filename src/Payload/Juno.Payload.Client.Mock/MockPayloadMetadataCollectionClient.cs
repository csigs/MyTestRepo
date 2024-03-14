
namespace Juno.Payload.Client.Mock
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Localization.Juno.Common.Data.Repository;

    using Juno.Payload.Client.Abstraction;
    using Juno.Payload.Client.Mock.Model;

    public class MockPayloadMetadataCollectionClient<TPayloadMetadataCollectionItem>
        : IPayloadMetadataCollectionClient<TPayloadMetadataCollectionItem>
        where TPayloadMetadataCollectionItem : class
    {
        public string PayloadPartitionKey { get; }

        public Guid PayloadId { get; }

        private readonly IRepository _metadataRepository;

        public MockPayloadMetadataCollectionClient(string partitionKey, Guid payloadId, IRepository metadataRepository)
        {
            if (string.IsNullOrWhiteSpace(partitionKey))
            {
                throw new ArgumentNullException(nameof(partitionKey));
            }

            if (payloadId == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(payloadId));
            }

            PayloadPartitionKey = partitionKey;
            PayloadId = payloadId;
           _metadataRepository = metadataRepository ?? throw new ArgumentNullException(nameof(metadataRepository));
        }

        public Task AddPayloadMetadataCollectionItemsAsync(IEnumerable<TPayloadMetadataCollectionItem> metadata, bool useTransaction = true, CancellationToken cancellationToken = default)
        {
            var property = metadata.First().GetType().GetProperty("Id");

            var docs = metadata.Select(m => new PayloadMetadata<TPayloadMetadataCollectionItem>()
            {
                Id = property != null ? property.GetValue(m).ToString() : Guid.NewGuid().ToString(),
                PayloadId = PayloadId,
                Metadata = m,
                CreatedTime = DateTimeOffset.UtcNow,
                UpdatedTime = DateTimeOffset.UtcNow
            });

            var metadataWithPartition = docs.Select(m => ItemWithPartition<PayloadMetadata<TPayloadMetadataCollectionItem>>.Create
                (m, PayloadId, PayloadPartitionKey));

            return _metadataRepository.CreateItemsAsync(metadataWithPartition);
        }

        public Task DeleteMetadataCollectionAsync(CancellationToken cancellationToken = default)
        {
            return _metadataRepository.DeleteItemsAsync<ItemWithPartition<PayloadMetadata<TPayloadMetadataCollectionItem>>>(m =>
                m.PartitionKey == PayloadPartitionKey && m.PayloadId == PayloadId);
        }

#pragma warning disable CS8425 // Async-iterator member has one or more parameters of type 'CancellationToken' but none of them is decorated with the 'EnumeratorCancellation' attribute, so the cancellation token parameter from the generated 'IAsyncEnumerable<>.GetAsyncEnumerator' will be unconsumed
        public async IAsyncEnumerable<TPayloadMetadataCollectionItem> ReadPayloadMetadataCollectionAsync(CancellationToken cancellationToken = default)
#pragma warning restore CS8425 // Async-iterator member has one or more parameters of type 'CancellationToken' but none of them is decorated with the 'EnumeratorCancellation' attribute, so the cancellation token parameter from the generated 'IAsyncEnumerable<>.GetAsyncEnumerator' will be unconsumed
        {
            var payloadMetadatas = await _metadataRepository.GetItemsAsync<ItemWithPartition<PayloadMetadata<TPayloadMetadataCollectionItem>>>(m
                => m.PartitionKey == PayloadPartitionKey && m.PayloadId == PayloadId);

            foreach( var item in  payloadMetadatas.Select(m => m.Data))
            {
                yield return item.Metadata;
            }
        }

        public async Task<IEnumerable<TPayloadMetadataCollectionItem>> ReadPayloadMetadataCollectionForAsync(IEnumerable<Guid> metadataItemIds, CancellationToken cancellationToken = default)
        {
            var ids = metadataItemIds.Select(i => i.ToString());

            var payloadMetadatas = await _metadataRepository.GetItemsAsync<ItemWithPartition<PayloadMetadata<TPayloadMetadataCollectionItem>>>(m
                => m.PartitionKey == PayloadPartitionKey 
                && m.PayloadId == PayloadId
                && ids.Contains(m.Data.Id));

            return payloadMetadatas.Select(m => m.Data.Metadata);
        }

        public async Task UpdatePayloadMetadataCollectionItemsAsync(IEnumerable<TPayloadMetadataCollectionItem> metadata, CancellationToken cancellationToken = default)
        {
            var metadataToUpdate = metadata.Where(m => m.GetType().GetProperty("Id") != null)
                            .Select(m => new
                            {
                                Id = m.GetType().GetProperty("Id").GetValue(m).ToString(),
                                Metadata = m
                            });

            var payloadMetadatas = await _metadataRepository.GetItemsAsync<ItemWithPartition<PayloadMetadata<TPayloadMetadataCollectionItem>>>(
                                        m => m.PartitionKey == PayloadPartitionKey
                                        && m.PayloadId == PayloadId 
                                        && metadataToUpdate.Any(i => i.Id == m.Data.Id))
                .ConfigureAwait(false);

            var updateDocs = payloadMetadatas.Join(metadataToUpdate,
                pm => pm.Data.Id,
                mu => mu.Id,
                (pm, mu) => new
                {
                    PayloadMetadata = pm,
                    Metadata = mu.Metadata
                }).ToList();

            foreach (var doc in updateDocs)
            {
                doc.PayloadMetadata.Data.Metadata = doc.Metadata;
            }

            var result = await _metadataRepository.UpdateItemsAsync(updateDocs.Select(d => d.PayloadMetadata)).ConfigureAwait(false);
        }
    }
}
