using System;
using System.Collections.Generic;
using System.Text;

namespace Juno.Payload.Service.Repository
{
    public class PayloadBlobAttachmentRepositoryFactory
    {

        public static IPayloadAttachmentRepository Create()
        {
            var attachmentStorageConnStr = Environment.GetEnvironmentVariable("AttachmentBlobStorageConnStr", EnvironmentVariableTarget.Process);
            var blobContainerName = Environment.GetEnvironmentVariable("AttachmentBlobStorageContainerName", EnvironmentVariableTarget.Process);
            return new PayloadBlobAttachmentRepository(attachmentStorageConnStr, blobContainerName);
        }
    }
}
