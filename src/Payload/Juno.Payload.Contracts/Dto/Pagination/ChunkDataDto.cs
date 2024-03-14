using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Juno.Payload.Contracts.Dto.Pagination
{
    public class DataChunkDto<TData>
    {
        [JsonConstructor]
        public DataChunkDto(string continuationToken, IEnumerable<TData> items)
        {
            ContinuationToken = continuationToken;
            Items = items ?? throw new ArgumentNullException(nameof(items));
        }

        [JsonProperty("continuationToken")]
        public string ContinuationToken { get; private set; }

        [JsonProperty("items")]
        public IEnumerable<TData> Items { get; private set; }
    }
}
