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
    using System.Net.Mime;
    using Newtonsoft.Json.Linq;

    public class PayloadWithData : PayloadWithInlineMetadata<JObject>
    {
        public static PayloadWithData CreateNew(string category = DefaultCategory, string partitionKey = DefaultPartitionKeyValue, PayloadVersion payloadVersion = PayloadVersion.V2)
        {
            var payload = new PayloadWithData();
            payload.InitializeNewWithoutInlineMetadataOnly(null, category, partitionKey, payloadVersion);
            return payload;
        }

        public static PayloadWithData CreateNew(Guid id, string category = DefaultCategory, string partitionKey = DefaultPartitionKeyValue, PayloadVersion payloadVersion = PayloadVersion.V2)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Value can't be empty guid. Pass null or valid non empty guid", nameof(id));
            }

            var payload = new PayloadWithData();
            payload.InitializeNewWithoutInlineMetadataOnly(id, category, partitionKey, payloadVersion);
            return payload;
        }

        public void UpdatePayloadInlineMetadataWith(PayloadDataType payloadDataType, object inlineMetadataObject)
        {
            if (inlineMetadataObject is null)
            {
                throw new ArgumentNullException(nameof(inlineMetadataObject));
            }

            switch (payloadDataType)
            {
                case PayloadDataType.BlobAttachment:
                    if(inlineMetadataObject is not BlobAttachmentReference)
                    {
                        throw new InvalidOperationException($"inlineMetadataObject need to be {nameof(BlobAttachmentReference)}");
                    }
                    UpdatePayloadDataReferenceWith((BlobAttachmentReference)inlineMetadataObject);
                    break;
                case PayloadDataType.InlineMetadata:
                    this.Metadata = JObject.FromObject(inlineMetadataObject);
                    this.MetadataType = inlineMetadataObject.GetType().Name;
                    this.PayloadDataType = PayloadDataType.InlineMetadata;
                    this.UpdatedTime = DateTimeOffset.UtcNow;
                    break;
                case PayloadDataType.BinaryBlobAttachment:

                    throw new NotImplementedException();
                case PayloadDataType.None:
                    throw new InvalidOperationException("Can't update non payload data type!");
                default:
                    throw new ArgumentException(payloadDataType.ToString());
            }
        }

        public void UpdatePayloadDataReferenceWith(BlobAttachmentReference blobAttachmentReference)
        {
            if (blobAttachmentReference is null)
            {
                throw new ArgumentNullException(nameof(blobAttachmentReference));
            }

            this.Metadata = JObject.FromObject(blobAttachmentReference);
            this.MetadataType = Constants.PayloadInlineMetadataAsBlobAttachmentType; // backwards compatibility with v1 api
            this.PayloadDataType = PayloadDataType.BlobAttachment;
            this.UpdatedTime = DateTimeOffset.UtcNow;
        }

        public bool HasBlobAttachment()
        {
            switch (PayloadDataType)
            {
                case PayloadDataType.BlobAttachment:
                case PayloadDataType.BinaryBlobAttachment:
                    return true;
                case PayloadDataType.InlineMetadata:
                default:
                    return false;
            }
        }

        public bool TryGetBlobAttachmentReference(out BlobAttachmentReference attachmentReference)
        {
            if (this.HasBlobAttachment())
            {
                attachmentReference = this.Metadata.ToObject<BlobAttachmentReference>();
                return true;
            }
            else
            {
                attachmentReference = null;
                return false;
            }
        }
    }
}
