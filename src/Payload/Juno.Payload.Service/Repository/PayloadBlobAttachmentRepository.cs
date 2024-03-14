// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UpdatePayloadInlineMetadata.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//   Upload payload's inline metadata to database. Use UpdatePayloadMetadata to upload a collection of metadata.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.IO;
using System.Threading.Tasks;
using Juno.Payload.Service.Model;
using Azure.Storage.Blobs;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage;

namespace Juno.Payload.Service
{
    public class PayloadBlobAttachmentRepository : IPayloadAttachmentRepository
    {
        private CloudBlobContainer _container;

        public PayloadBlobAttachmentRepository(string storageConnectionString, string containerName)
        {
            if (string.IsNullOrWhiteSpace(storageConnectionString))
            {
                throw new System.ArgumentException("message", nameof(storageConnectionString));
            }

            if (string.IsNullOrWhiteSpace(containerName))
            {
                throw new System.ArgumentException("message", nameof(containerName));
            }

            var storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            var myClient = storageAccount.CreateCloudBlobClient();
            var container = myClient.GetContainerReference(containerName);
            container.CreateIfNotExists();            
            _container = container;
        }

        public async Task<bool> ExistsAsync(BlobAttachmentReference blobAttachment)
        {
            var blobReference = _container.GetBlockBlobReference(blobAttachment.AttachmentId);
            return await blobReference.ExistsAsync();
        }

        public async Task DeleteAttachmentAsync(BlobAttachmentReference blobAttachment)
        {
            var blobReference = _container.GetBlockBlobReference(blobAttachment.AttachmentId);
            await blobReference.DeleteAsync();
        }

        public async Task<Stream> GetAttachmentAsync(BlobAttachmentReference blobAttachment)
        {
            var blobReference = _container.GetBlockBlobReference(blobAttachment.AttachmentId);
            return await blobReference.OpenReadAsync();
        }

        public async Task UploadAttachmentAsync(BlobAttachmentReference blobAttachment, Stream data)
        {
            var blobReference = _container.GetBlockBlobReference(blobAttachment.AttachmentId);
            await blobReference.UploadFromStreamAsync(data);
        }
    }
}
