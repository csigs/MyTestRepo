
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

    public class MockPayloadClientV2 : IPayloadClientV2
    {
        public Guid PayloadId { get; set; }

        public Guid? FallbackBuildId { get; set; }

        public string PartitionKey { get; set; }

        private IRepository _payloadRepository;
        private IRepository _metadataRepository;
        private IRepository _dataRefRepository;

        public MockPayloadClientV2(
            string partitionKey,
            Guid payloadId,
            IRepository payloadRepository,
            IRepository metadataRepository,
            IRepository dataRefRepository)
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
            _payloadRepository = payloadRepository;
            _metadataRepository = metadataRepository;
            _dataRefRepository = dataRefRepository;
        }

        public async Task<bool> ExistsAsync(CancellationToken cancellationToken = default)
        {
            var payloadItems = await _payloadRepository.GetItemsAsync<ItemWithPartition<PayloadWithMetadata<object>>>(
                m => m.PartitionKey == PartitionKey
                && m.PayloadId == PayloadId);

            return payloadItems.Any();
        }

        public Task CreateNewAsync(string category = "Default", CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task DeleteAsync(CancellationToken cancellationToken = default)
        {
            await _payloadRepository.DeleteItemsAsync<ItemWithPartition<PayloadWithMetadata<object>>>(
                m => m.PartitionKey == PartitionKey
                && m.PayloadId == PayloadId);
        }

        public IPayloadDataClient<TPayloadData> GetPayloadDataClient<TPayloadData>() where TPayloadData : class
        {
            return new MockPayloadDataClient<TPayloadData>(PartitionKey, PayloadId, _payloadRepository);
        }

        public IPayloadMetadataCollectionClient<TMetadataCollectionItem> GetPayloadMetadataCollectionClient<TMetadataCollectionItem>() where TMetadataCollectionItem : class
        {
            return new MockPayloadMetadataCollectionClient<TMetadataCollectionItem>(PartitionKey, PayloadId, _metadataRepository);
        }

        public IPayloadDataReferencesClient GetPayloadDataReferencesClient()
        {
            return new MockPayloadDataReferencesClient(PartitionKey, PayloadId, _dataRefRepository);
        }
    }
}
