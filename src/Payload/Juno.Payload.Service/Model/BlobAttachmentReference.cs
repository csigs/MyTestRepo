using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Mime;
using System.Text;
using Newtonsoft.Json;

namespace Juno.Payload.Service.Model
{
    public class BlobAttachmentReference
    {
        public BlobAttachmentReference(BlobAttachmentId blobAttachmentId, ContentType contentType, string contentEncoding = null):this(blobAttachmentId.ToString(), contentType.ToString(), contentEncoding)
        {
        }

        [JsonConstructor]
        public BlobAttachmentReference(string attachmentId, string attachmentContentType, string attachmentContentEncoding)
        {
            if (string.IsNullOrWhiteSpace(attachmentId))
            {
                throw new ArgumentException("message", nameof(attachmentId));
            }

            AttachmentId = attachmentId;
            AttachmentContentType = attachmentContentType;
            AttachmentContentEncoding = attachmentContentEncoding;
        }

        [JsonProperty("attachmentId")]
        public string AttachmentId { get; private set; }

        [JsonProperty("attachmentContentType")]
        public string AttachmentContentType { get; private set; }

        [JsonProperty("attachmentContentEncoding")]
        public string AttachmentContentEncoding { get; private set; }
    }
}
