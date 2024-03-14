

namespace Juno.Payload.Client.Mock
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.Localization.Juno.Common.Data.Repository;
    
    using Juno.Payload.Client.Abstraction;
    using Juno.Payload.Client.Abstraction.V1;
    using Juno.Payload.Client.Mock.Model;
    using Juno.Payload.Dto;

    public class MockPayloadApiClient : IPayloadApiClient
    {
        public MockListRepository PayloadRepository { get; }  = new MockListRepository();
        public MockListRepository MetadataRepository { get; } = new MockListRepository();
        public MockListRepository DataRefRepository { get; } = new MockListRepository();

        [Obsolete]
        public async Task<IPayloadClient<TInlineMetadata, TMetadata>> CreatePayloadAsync<TInlineMetadata, TMetadata>()
            where TInlineMetadata : class
            where TMetadata : class
        {
            var payloadId = Guid.NewGuid();

            var payload = new PayloadWithMetadata<TInlineMetadata>
            {
                Id = payloadId.ToString(),
                Category = "Default",
                Metadata = null,
                CreatedTime = DateTimeOffset.UtcNow,
                UpdatedTime = DateTimeOffset.UtcNow
            };

            await PayloadRepository.CreateItemAsync(payload);

            return new MockPayloadClient<TInlineMetadata, TMetadata>(
                PayloadRepository,
                MetadataRepository,
                DataRefRepository,
                payloadId);
        }

        public async Task<IPayloadClientV2> CreatePayloadV2Async<TPayloadData>(string partitionKey, string category = "Default") where TPayloadData : class
        {
            var payloadId = Guid.NewGuid();

            var payload = new PayloadWithMetadata<TPayloadData>
            {
                Id = payloadId.ToString(),
                Category = "Default",
                Metadata = null,
                CreatedTime = DateTimeOffset.UtcNow,
                UpdatedTime = DateTimeOffset.UtcNow
            };

            await PayloadRepository.CreateItemAsync(ItemWithPartition<PayloadWithMetadata<TPayloadData>>.Create
                (payload, payloadId, partitionKey));

            return new MockPayloadClientV2(
                partitionKey,
                payloadId,
                PayloadRepository,
                MetadataRepository,
                DataRefRepository);
        }

        [Obsolete]
        public async Task<IPayloadClient<TInlineMetadata, TMetadata>> CreatePayloadWithDataAsync<TInlineMetadata, TMetadata>(TInlineMetadata payloadMetadata, string category = "Default", TimeSpan uploadTimeout = default)
            where TInlineMetadata : class
            where TMetadata : class
        {
            var payloadId = Guid.NewGuid();

            var payload = new PayloadWithMetadata<TInlineMetadata>
            {
                Id = payloadId.ToString(),
                Category = "Default",
                Metadata = payloadMetadata,
                CreatedTime = DateTimeOffset.UtcNow,
                UpdatedTime = DateTimeOffset.UtcNow
            };

            await PayloadRepository.CreateItemAsync(payload);

            return new MockPayloadClient<TInlineMetadata, TMetadata>(
                PayloadRepository,
                MetadataRepository,
                DataRefRepository,
                payloadId);
        }

        public async Task<IPayloadClientV2> CreatePayloadWithDataV2Async<TPayloadData>(string partitionKey, TPayloadData payloadData, string category = "Default", PayloadDataStorageTypeDto payloadDataType = PayloadDataStorageTypeDto.BlobAttachment, TimeSpan uploadTimeout = default) where TPayloadData : class
        {
            var payloadId = Guid.NewGuid();

            var payload = new PayloadWithMetadata<TPayloadData>
            {
                Id = payloadId.ToString(),
                Category = "Default",
                Metadata = payloadData,
                CreatedTime = DateTimeOffset.UtcNow,
                UpdatedTime = DateTimeOffset.UtcNow
            };

            await PayloadRepository.CreateItemAsync(ItemWithPartition<PayloadWithMetadata<TPayloadData>>.Create
                (payload, payloadId, partitionKey));

            return new MockPayloadClientV2(
                partitionKey,
                payloadId,
                PayloadRepository,
                MetadataRepository,
                DataRefRepository);
        }

        public async Task DeletePayloadAsync(Guid payloadId)
        {
            await PayloadRepository.DeleteItemAsync(payloadId.ToString()).ConfigureAwait(false);
        }

        public Task DeletePayloadDataReferencesAsync(Guid payloadId, string partitionKey)
        {
            var dataRefClient = new MockPayloadDataReferencesClient(partitionKey, payloadId, DataRefRepository);

            return dataRefClient.DeleteDataReferencesAsync();
        }

        public async Task DeletePayloadMetadataAsync(Guid payloadId)
        {
            await MetadataRepository.DeleteItemAsync(payloadId.ToString()).ConfigureAwait(false);
        }

        public Task DeletePayloadMetadataCollectionAsync(Guid payloadId, string partitionKey)
        {
            return MetadataRepository.DeleteItemsAsync<ItemWithPartition<PayloadMetadata<object>>>(m =>
                m.PartitionKey == partitionKey && m.PayloadId == payloadId);
        }

        public Task DeletePayloadAsync(Guid payloadId, string partitionKey)
        {
            return PayloadRepository.DeleteItemsAsync<ItemWithPartition<PayloadMetadata<object>>>(m =>
                 m.PartitionKey == partitionKey && m.PayloadId == payloadId);
        }

        [Obsolete]
        public IPayloadClient<TInlineMetadata, TMetadata> GetPayloadClient<TInlineMetadata, TMetadata>(Guid payloadId, Guid? fallbackBuildId = null)
            where TInlineMetadata : class
            where TMetadata : class
        {
            if (payloadId == Guid.Empty)
            {
                throw new ArgumentException("Value can't be empty", nameof(payloadId));
            }

            return new MockPayloadClient<TInlineMetadata, TMetadata>(
                PayloadRepository,
                MetadataRepository,
                DataRefRepository,
                payloadId);
        }

        public IPayloadClientV2 GetPayloadClient(Guid payloadId, string partitionKey)
        {
            if (payloadId == Guid.Empty)
            {
                throw new ArgumentException("Value can't be empty", nameof(payloadId));
            }

            return new MockPayloadClientV2(partitionKey, payloadId, PayloadRepository, MetadataRepository, DataRefRepository);
        }

        [Obsolete]
        public IPayloadReadClient<TInlineMetadata, TMetadata> GetPayloadReadClient<TInlineMetadata, TMetadata>(Guid payloadId, Guid? fallbackBuildId = null)
            where TInlineMetadata : class
            where TMetadata : class
        {
            return new MockPayloadClient<TInlineMetadata, TMetadata>(
                PayloadRepository,
                MetadataRepository,
                DataRefRepository,
                payloadId);
        }
    }
}
