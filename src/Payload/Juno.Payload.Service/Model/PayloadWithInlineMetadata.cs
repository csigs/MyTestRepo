// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PayloadWithMetadata.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//   Payload model when a payload has inline metadata. Metadata size should not exceeds 2MB.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Juno.Payload.Service.Model
{
    using System;
    using System.ComponentModel;
    using Newtonsoft.Json;

    /// <summary>
    /// Defines payload model with inline metadata for payload.
    /// </summary>
    /// <typeparam name="TMetadata"></typeparam>
    public class PayloadWithInlineMetadata<TInlineMetadataType> : PayloadBase where TInlineMetadataType : class
    {
        public const string DefaultPartitionKeyValue = "DefaultPartition";

        public const string DefaultCategory = "Default";

        public PayloadWithInlineMetadata()
        {
            MetadataType = typeof(TInlineMetadataType).Name;
            PartitionKey = DefaultPartitionKeyValue;
        }

        [DefaultValue(PayloadDataType.BlobAttachment)] // for backwards compatibility where all payload data is stored as blob attachment
        [JsonProperty("payloadDataType", DefaultValueHandling = DefaultValueHandling.Populate)]
        public PayloadDataType PayloadDataType { get; set; }

        [JsonProperty("metadataType")]
        public string MetadataType { get; set; }

        [JsonProperty("metadata")]
        public TInlineMetadataType Metadata { get; set; }

        protected void InitializeNewWithoutInlineMetadataOnly(Guid? id, string category = DefaultCategory, string partitionKey = DefaultPartitionKeyValue, PayloadVersion payloadVersion = PayloadVersion.V2)
        {
            if(!string.IsNullOrEmpty(Id))
            {
                throw new InvalidOperationException("Object already initialized. Can't initialize again");
            }

            if (id != null && id == Guid.Empty)
            {
                throw new ArgumentException("Value can't be empty guid. Pass null or valid non empty guid", nameof(id));
            }
            

            Id = id?.ToString() ?? Guid.NewGuid().ToString();
            Category = category;
            PayloadVersion = payloadVersion;
            Metadata = null;
            PartitionKey = partitionKey;
            CreatedTime = DateTimeOffset.UtcNow;
            UpdatedTime = DateTimeOffset.UtcNow;
        }
    }
}
