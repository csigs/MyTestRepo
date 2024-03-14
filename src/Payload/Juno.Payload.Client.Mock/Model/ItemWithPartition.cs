
namespace Juno.Payload.Client.Mock.Model
{
    using System;
    using Newtonsoft.Json;

    public class ItemWithPartition<TData>
    {
        [JsonProperty("payloadId")]
        public Guid PayloadId { get; set; }

        [JsonProperty("partitionKey")]
        public string PartitionKey { get; set; }

        [JsonProperty("data")]
        public TData Data { get; set; }

        public static ItemWithPartition<TData> Create(TData data, Guid payloadId, string partitionKey)
        {
            return new ItemWithPartition<TData>()
            {
                PayloadId = payloadId,
                PartitionKey = partitionKey,
                Data = data
            };
        }
    }
}
