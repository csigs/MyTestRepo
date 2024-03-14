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
    public class PayloadMetadataCollectionItem<TMetadata> : IIdentifiableObject
        where TMetadata : class
    {
        public PayloadMetadataCollectionItem()
        {
            MetadataType = typeof(TMetadata).Name;
        }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("payloadId")]
        public Guid PayloadId { get; set; }

        [JsonProperty("metadataType")]
        public string MetadataType { get; set; }

        /// <summary>
        /// Gets or sets the created date of a payload.
        /// </summary>
        [JsonProperty("createdTime")]
        public DateTimeOffset CreatedTime { get; set; }

        /// <summary>
        /// Gets or sets the created date of a payload.
        /// </summary>
        [JsonProperty("updatedTime")]
        public DateTimeOffset UpdatedTime { get; set; }

        [JsonProperty("metadata")]
        public TMetadata Metadata { get; set; }
        
    }
}
