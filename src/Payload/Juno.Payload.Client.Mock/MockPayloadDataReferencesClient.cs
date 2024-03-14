
namespace Juno.Payload.Client.Mock
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Localization.Juno.Common.Data.Repository;

    using Juno.Common.DataReference;
    using Juno.Payload.Client.Abstraction;
    using Juno.Payload.Client.Mock.Model;

    public class MockPayloadDataReferencesClient : IPayloadDataReferencesClient
    {
        public string PayloadPartitionKey { get; }

        public Guid PayloadId { get; }

        private readonly IRepository _dataRefRepository;

        public MockPayloadDataReferencesClient(string partitionKey, Guid payloadId, IRepository dataRefRepository)
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
            _dataRefRepository = dataRefRepository ?? throw new ArgumentException(nameof(dataRefRepository));
        }

        public Task AddLocElementDataReferencesAsync(IEnumerable<LocElementDataReferenceDescriptor> dataReferenceDescriptors, bool useTransaction = true, CancellationToken cancellationToken = default)
        {
            var dataRefs = dataReferenceDescriptors.Select(d => new PayloadData<LocElementDataReferenceDescriptor>()
            {
                Id = Guid.NewGuid().ToString(),
                PayloadId = PayloadId.ToString(),
                ProvidedId = d.LocElementMetadata.GroupId.ToString(),
                Data = d
            });

            var metadataWithPartition = dataRefs.Select(m => ItemWithPartition<PayloadData<LocElementDataReferenceDescriptor>>.Create
                (m, PayloadId, PayloadPartitionKey));

            return _dataRefRepository.CreateItemsAsync(metadataWithPartition);
        }

        public Task DeleteDataReferencesAsync(CancellationToken cancellationToken = default)
        {
            return _dataRefRepository.DeleteItemsAsync<ItemWithPartition<PayloadData<LocElementDataReferenceDescriptor>>>(m =>
                m.PartitionKey == PayloadPartitionKey && m.PayloadId == PayloadId);
        }

#pragma warning disable CS8425 // Async-iterator member has one or more parameters of type 'CancellationToken' but none of them is decorated with the 'EnumeratorCancellation' attribute, so the cancellation token parameter from the generated 'IAsyncEnumerable<>.GetAsyncEnumerator' will be unconsumed
        public async IAsyncEnumerable<LocElementDataReferenceDescriptor> ReadStoredLocElementDataReferencesAsync(CancellationToken cancellationToken = default)
#pragma warning restore CS8425 // Async-iterator member has one or more parameters of type 'CancellationToken' but none of them is decorated with the 'EnumeratorCancellation' attribute, so the cancellation token parameter from the generated 'IAsyncEnumerable<>.GetAsyncEnumerator' will be unconsumed
        {
            var payloadMetadatas = await _dataRefRepository.GetItemsAsync<ItemWithPartition<PayloadData<LocElementDataReferenceDescriptor>>>(m
                => m.PartitionKey == PayloadPartitionKey && m.PayloadId == PayloadId);

            foreach (var item in payloadMetadatas.Select(m => m.Data))
            {
                yield return item.Data;
            }
        }

        public async Task<IEnumerable<LocElementDataReferenceDescriptor>> ReadStoredLocElementDataReferencesChunkForAsync(IEnumerable<Guid> locElementMetadataIds, CancellationToken cancellationToken = default)
        {
            var ids = locElementMetadataIds.Select(i => i.ToString());

            var payloadMetadatas = await _dataRefRepository.GetItemsAsync<ItemWithPartition<PayloadData<LocElementDataReferenceDescriptor>>>(m
                => m.PartitionKey == PayloadPartitionKey
                && m.PayloadId == PayloadId
                && ids.Contains(m.Data.Id));

            return payloadMetadatas.Select(m => m.Data.Data);
        }
    }
}
