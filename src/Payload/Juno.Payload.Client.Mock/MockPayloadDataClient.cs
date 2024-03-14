
namespace Juno.Payload.Client.Mock
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Localization.Juno.Common.Data.Repository;

    using Juno.Payload.Client.Abstraction;
    using Juno.Payload.Client.Mock.Model;
    using Juno.Payload.Dto;

    public class MockPayloadDataClient<TPayloadData> : IPayloadDataClient<TPayloadData> where TPayloadData : class
    {
        public string PayloadPartitionKey { get; }

        public Guid PayloadId { get; }

        public IRepository PayloadRepository { get; }

        private readonly IRepository _payloadRepository;

        public MockPayloadDataClient(string partitionKey, Guid payloadId, IRepository payloadRepository)
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
            _payloadRepository = payloadRepository ?? throw new ArgumentException(nameof(payloadRepository));
        }

        public async Task<TPayloadData> ReadPayloadDataAsync(CancellationToken cancellationToken = default)
        {
            var payloadItems = await _payloadRepository.GetItemsAsync<ItemWithPartition<PayloadWithMetadata<TPayloadData>>>(
                m => m.PartitionKey == PayloadPartitionKey
                && m.PayloadId == PayloadId);

            return payloadItems.FirstOrDefault()?.Data.Metadata ?? throw new Exception($"Payload with {PayloadId} not found.");
        }

        public async Task UploadPayloadDataAsync(TPayloadData payloadData, PayloadDataStorageTypeDto payloadDataStorageType = PayloadDataStorageTypeDto.BlobAttachment, TimeSpan uploadTimeout = default, CancellationToken cancellationToken = default)
        {
            var payloads = await _payloadRepository.GetItemsAsync<ItemWithPartition<PayloadWithMetadata<TPayloadData>>>(
                m => m.PartitionKey == PayloadPartitionKey
                && m.PayloadId == PayloadId);

            var payload = payloads.FirstOrDefault();

            if (payload == null)
            {
                throw new Exception("item not found");
            }

            payload.Data.Metadata = payloadData;
            payload.Data.UpdatedTime = DateTimeOffset.UtcNow;
        }
    }
}
