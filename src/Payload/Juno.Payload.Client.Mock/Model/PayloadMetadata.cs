using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Juno.Payload.Client.Mock.Model
{
    public class PayloadMetadata<TMetadata> : PayloadBase where TMetadata : class
    {
        public PayloadMetadata()
        {
            MetadataType = typeof(TMetadata).Name;
        }

        [JsonProperty("payloadId")]
        public Guid PayloadId { get; set; }

        [JsonProperty("metadataType")]
        public string MetadataType { get; set; }

        [JsonProperty("metadata")]
        public TMetadata Metadata { get; set; }
    }
}
