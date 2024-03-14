// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PayloadBase.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//   Basic payload model.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Juno.Payload.Service.Model
{
    using System;
    using System.ComponentModel;
    using Newtonsoft.Json;

    /// <summary>
    /// Basic payload model that has essential data contract.
    /// </summary>
    public abstract class PayloadBase : IIdentifiableObject
    {
        /// <summary>
        /// Gets or sets the payload Id.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("partitionKey")]
        public string PartitionKey { get; set; }

        [JsonProperty("payloadVersion", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(PayloadVersion.V1)]
        public PayloadVersion PayloadVersion { get; set; }

        /// <summary>
        /// Gets or sets the category of a payload.
        /// </summary>
        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("metadataCollectionId")]
        public string MetadataCollectionId { get; set; }

        [JsonProperty("dataReferenceCollectionId")]
        public string DataReferenceCollectionId { get; set; }

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
    }
}
