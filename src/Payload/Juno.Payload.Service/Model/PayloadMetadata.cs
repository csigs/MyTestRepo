using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Juno.Payload.Service.Model
{
    /// <summary>
    /// Defines metadata model stored in separate payload metadata collection for payload referenced by <see cref="PayloadId"/> with possible relationship for payload - metadata => 1:N.  
    /// </summary>
    /// <typeparam name="TMetadata"></typeparam>
    public class PayloadMetadata<TMetadata>: PayloadBase where TMetadata : class
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
