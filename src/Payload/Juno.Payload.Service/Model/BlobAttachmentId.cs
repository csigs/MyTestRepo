// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UpdatePayloadInlineMetadata.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//   Upload payload's inline metadata to database. Use UpdatePayloadMetadata to upload a collection of metadata.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Reflection.Metadata.Ecma335;
using Newtonsoft.Json.Linq;

namespace Juno.Payload.Service.Model
{
    public class BlobAttachmentId
    {
        private readonly string _id;

        public BlobAttachmentId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("message", nameof(id));
            }

            _id = id;
        }

        public override string ToString()
        {
            return this._id;
        }

        public static BlobAttachmentId CreateFor(PayloadBase storedPayload)
        {
            if (storedPayload is null)
            {
                throw new ArgumentNullException(nameof(storedPayload));
            }
            
            return new BlobAttachmentId(storedPayload.Id + "_" + Guid.NewGuid().ToString());
        }
    }
}
