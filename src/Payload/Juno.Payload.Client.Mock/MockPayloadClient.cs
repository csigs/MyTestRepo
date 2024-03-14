
namespace Juno.Payload.Client.Mock
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Localization.Juno.Common.Data.Repository;

    using Juno.Common.DataReference;
    using Juno.Common.Metadata;
    using Juno.Payload.Client.Abstraction.V1;
    using Payload.Client.Mock.Model;

    [Obsolete]
    public class MockPayloadClient<TInlineMetadata, TMetadata> : IPayloadClient<TInlineMetadata, TMetadata>
        where TInlineMetadata : class
        where TMetadata : class
    {
        public Guid PayloadId { get; set; }

        public Guid? FallbackBuildId { get; set; }

        private IRepository _payloadRepository;
        private IRepository _metadataRepository;
        private IRepository _dataRefRepository;

        public MockPayloadClient(IRepository payloadRepository, IRepository metadataRepository, IRepository dataRefRepository, Guid payloadId)
        {
            _payloadRepository = payloadRepository;
            _metadataRepository = metadataRepository;
            _dataRefRepository = dataRefRepository;
            PayloadId = payloadId;
        }

        public async Task<bool> ExistsAsync(CancellationToken cancellationToken)
        {
            var payload = await _payloadRepository.GetItemAsync<PayloadWithMetadata<TInlineMetadata>>(PayloadId.ToString());

            if (payload == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public async Task<TInlineMetadata> GetInlineMetadataAsync(CancellationToken cancellationToken)
        {
            var payload = await _payloadRepository.GetItemAsync<PayloadWithMetadata<TInlineMetadata>>(PayloadId.ToString());

            if (payload == null)
            {
                throw new Exception($"Payload with {PayloadId} not found.");
            }

            return payload.Metadata;
        }

        public async Task<IEnumerable<TMetadata>> GetPayloadMetadataAsync(CancellationToken cancellationToken)
        {
            var payloadMetadatas = await _metadataRepository.GetItemsAsync<PayloadMetadata<TMetadata>>(m
                => m.PayloadId == PayloadId);

            if (!payloadMetadatas.Any())
            {
                return Enumerable.Empty<TMetadata>();
            }

            return payloadMetadatas.Select(m => m.Metadata);
        }

        public async Task<IEnumerable<TMetadata>> GetPayloadMetadataAsync(List<Guid> providedIds, CancellationToken cancellationToken)
        {
            var ids = providedIds.Select(i => i.ToString());

            var payloadMetadatas = await _metadataRepository.GetItemsAsync<PayloadMetadata<TMetadata>>(m
                => m.PayloadId == PayloadId
                && ids.Contains(m.Id));

            if (!payloadMetadatas.Any())
            {
                return Enumerable.Empty<TMetadata>();
            }

            return payloadMetadatas.Select(m => m.Metadata);
        }

        public async Task<IEnumerable<LocElementDataReferenceDescriptor>> GetLocElementDataReferencesAsync(IEnumerable<ILocElementMetadata> locElements, CancellationToken cancellationToken)
        {
            var ids = locElements.Select(e => e.Id).ToArray();
            return await GetLocElementDataReferencesAsync(ids, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IEnumerable<LocElementDataReferenceDescriptor>> GetLocElementDataReferencesAsync(IEnumerable<Guid> locElementIds, CancellationToken cancellationToken)
        {
            var ids = locElementIds.Select(e => e.ToString());

            var dataRefs = await _dataRefRepository.GetItemsAsync<PayloadData<LocElementDataReferenceDescriptor>>(
                r => r.PayloadId == PayloadId.ToString()
                && ids.Contains(r.ProvidedId)).ConfigureAwait(false);

            return dataRefs.Where(d => d.Data != null).Select(d => d.Data);
        }

        public async Task<IEnumerable<LocElementDataReferenceDescriptor>> GetStoredLocElementDataReferencesAsync(CancellationToken cancellationToken)
        {
            var dataRefs = await _dataRefRepository.GetItemsAsync<PayloadData<LocElementDataReferenceDescriptor>>(
                r => r.PayloadId == PayloadId.ToString()).ConfigureAwait(false);

            return dataRefs.Where(d => d.Data != null).Select(d => d.Data);
        }

        public async Task<IEnumerable<LocElementDataReferenceDescriptor>> GetStoredLocElementDataReferencesAsync(IEnumerable<ILocElementMetadata> locElementMetadata, CancellationToken cancellationToken)
        {
            return await GetLocElementDataReferencesAsync(locElementMetadata, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IEnumerable<LocElementDataReferenceDescriptor>> GetStoredLocElementDataReferencesAsync(IEnumerable<Guid> locElementIds, CancellationToken cancellationToken)
        {
            return await GetLocElementDataReferencesAsync(locElementIds, cancellationToken).ConfigureAwait(false);
        }

        public async Task UpdatePayloadInlineMetadataAsync(TInlineMetadata metadata, CancellationToken cancellationToken)
        {
            var storedPayload = await _payloadRepository.GetItemAsync<PayloadWithMetadata<TInlineMetadata>>(PayloadId.ToString()).ConfigureAwait(false);

            if (storedPayload == null)
            {
                throw new InvalidOperationException("Document was not created yet. Call CreatePayload first.");
            }
            else
            {
                storedPayload.Metadata = metadata;
                storedPayload.UpdatedTime = DateTimeOffset.UtcNow;

                await _payloadRepository.UpdateItemAsync(PayloadId.ToString(), storedPayload).ConfigureAwait(false);
            }
        }

        public async Task UpdatePayloadMetadataAsync(IEnumerable<TMetadata> metadata, CancellationToken cancellationToken)
        {
            var metadataToUpdate = metadata.Where(m => m.GetType().GetProperty("Id") != null)
                            .Select(m => new
                            {
                                Id = m.GetType().GetProperty("Id").GetValue(m).ToString(),
                                Metadata = m
                            });

            var payloadMetadatas = await _metadataRepository.GetItemsAsync<PayloadMetadata<TMetadata>>(
                                        m => metadataToUpdate.Any(i => i.Id == m.Id)).ConfigureAwait(false);

            var updateDocs = payloadMetadatas.Join(metadataToUpdate,
                pm => pm.Id,
                mu => mu.Id,
                (pm, mu) => new
                {
                    PayloadMetadata = pm,
                    Metadata = mu.Metadata
                }).ToList();

            foreach (var doc in updateDocs)
            {
                doc.PayloadMetadata.Metadata = doc.Metadata;
            }

            var result = await _metadataRepository.UpdateItemsAsync(updateDocs.Select(d => d.PayloadMetadata)).ConfigureAwait(false);
        }

        public async Task UploadDataReferencesAsync(IEnumerable<LocElementDataReferenceDescriptor> dataElements, bool useTransaction = false, CancellationToken cancellationToken = default)
        {
            var dataRefs = dataElements.Select(d => new PayloadData<LocElementDataReferenceDescriptor>()
            {
                Id = Guid.NewGuid().ToString(),
                PayloadId = PayloadId.ToString(),
                ProvidedId = d.LocElementMetadata.GroupId.ToString(),
                Data = d
            });

            await _dataRefRepository.CreateItemsAsync(dataRefs).ConfigureAwait(false);
        }

        public async Task UploadPayloadMetadataAsync(
            IEnumerable<TMetadata> metadata,
            bool useTransaction = false,
            CancellationToken cancellationToken = default)
        {
            var property = metadata.First().GetType().GetProperty("Id");

            var docs = metadata.Select(m => new PayloadMetadata<TMetadata>()
            {
                Id = property != null ? property.GetValue(m).ToString() : Guid.NewGuid().ToString(),
                PayloadId = PayloadId,
                Metadata = m,
                CreatedTime = DateTimeOffset.UtcNow,
                UpdatedTime = DateTimeOffset.UtcNow
            });

            await _metadataRepository.CreateItemsAsync(docs).ConfigureAwait(false);
        }
    }
}
