using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Juno.Payload.Service.Model;
using Juno.Payload.Service.Repository;
using Microsoft.Localization.Juno.Common.Data.Repository;

namespace Juno.Payload.Service.UnitTests
{
    public class MockPayloadRepository : MockListRepository, IPayloadRepository, IPayloadMetadataRepository, IPayloadDataRefRepository, IPayloadAttachmentRepository
    {
        private Dictionary<string, byte[]> _blobAttachmentRepoMock = new Dictionary<string, byte[]>();

        public Task<TPayload> CreatePayloadAsync<TPayload>(TPayload payload, CancellationToken cancellationToken = default) where TPayload : PayloadBase
        {
            return CreateItemAsync(payload);
        }

        public Task<bool> ExistsAsync(BlobAttachmentReference blobAttachment)
        {
            if (blobAttachment is null)
            {
                throw new ArgumentNullException(nameof(blobAttachment));
            }

            lock (_blobAttachmentRepoMock)
            {
                return Task.FromResult(_blobAttachmentRepoMock.ContainsKey(blobAttachment.AttachmentId));
            }
        }

        public Task<Stream> GetAttachmentAsync(BlobAttachmentReference blobAttachment)
        {
            if (blobAttachment is null)
            {
                throw new ArgumentNullException(nameof(blobAttachment));
            }

            return Task.FromResult<Stream>(new MemoryStream(_blobAttachmentRepoMock[blobAttachment.AttachmentId]));
        }

        public async Task<DataChunk<T>> GetItemsChunkAsync<T>(Expression<Func<T, bool>> predicate, string partitionKey, string continuationToken = null, CancellationToken cancellationToken = default) where T : class
        {
            var results = await GetItemsAsync(predicate);
            return new DataChunk<T>(null, results.ToList()); 
        }

        public async Task UploadAttachmentAsync(BlobAttachmentReference blobAttachment, Stream data)
        {
            if (blobAttachment is null)
            {
                throw new ArgumentNullException(nameof(blobAttachment));
            }

            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var memoryStream = new MemoryStream();
            await Task.Run(() => data.CopyTo(memoryStream));

            _blobAttachmentRepoMock[blobAttachment.AttachmentId] = memoryStream.ToArray();
        }

        public async Task DeleteItemsSteamAsync<T>(Expression<Func<T, bool>> predicate, string partitionKey, CancellationToken cancellationToken) where T : class, IIdentifiableObject
        {
            await DeleteItemsAsync(predicate);
        }

        public Task<IEnumerable<T>> GetItemsAsync<T>(Expression<Func<T, bool>> predicate, string partitionKey, CancellationToken cancellationToken = default) where T : class
        {
            return GetItemsAsync(predicate);
        }

        public Task DeleteAttachmentAsync(BlobAttachmentReference blobAttachmentReference)
        {
            lock (_blobAttachmentRepoMock)
            {
                if(_blobAttachmentRepoMock.ContainsKey(blobAttachmentReference.AttachmentId))
                {
                    _blobAttachmentRepoMock.Remove(blobAttachmentReference.AttachmentId);
                }
                return Task.CompletedTask;
            }
        }

        public async Task CreateItemsBatchAsync<T>(IEnumerable<T> items, string partitionKey, CancellationToken cancellationToken = default) where T : class, IIdentifiableObject
        {
            foreach(var item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await CreateItemAsync(item);
            }
        }
    }
}
