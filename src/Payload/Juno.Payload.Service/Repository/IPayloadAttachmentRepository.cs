// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UpdatePayloadInlineMetadata.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//   Upload payload's inline metadata to database. Use UpdatePayloadMetadata to upload a collection of metadata.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.IO;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using Juno.Payload.Service.Model;

namespace Juno.Payload.Service
{
    public interface IPayloadAttachmentRepository
    {   
        Task<bool> ExistsAsync(BlobAttachmentReference blobAttachmentReference);

        Task DeleteAttachmentAsync(BlobAttachmentReference blobAttachmentReference);

        Task<Stream> GetAttachmentAsync(BlobAttachmentReference blobAttachmentReference);

        Task UploadAttachmentAsync(BlobAttachmentReference blobAttachmentReference, Stream data);
    }
}
