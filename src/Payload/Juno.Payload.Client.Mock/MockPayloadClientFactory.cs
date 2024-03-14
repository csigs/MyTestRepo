

namespace Juno.Payload.Client.Mock
{
    using System;
    
    using Juno.Payload.Client.Abstraction.V1;

    using Microsoft.Localization.Juno.Common.Data.Repository;

    [Obsolete]
    public class MockPayloadClientFactory<TInlineMetadata, TMetadata> : IPayloadClientFactory<TInlineMetadata, TMetadata>
        where TInlineMetadata : class
        where TMetadata : class
    {
        private readonly IRepository _payloadRepository;
        private readonly IRepository _metadataRepository;
        private readonly IRepository _dataRefRepository;

        public MockPayloadClientFactory(IRepository payloadRepository, IRepository metadataRepository, IRepository dataRefRepository)
        {
            _payloadRepository = payloadRepository;
            _metadataRepository = metadataRepository;
            _dataRefRepository = dataRefRepository;
    }

        public IPayloadClient<TInlineMetadata, TMetadata> Create(
            Guid payloadId,
            Guid? fallbackBuildId = null)
        {
            return new MockPayloadClient<TInlineMetadata, TMetadata>(
                _payloadRepository?? new MockListRepository(),
                _metadataRepository?? new MockListRepository(),
                _dataRefRepository?? new MockListRepository(),
                payloadId);
        }

        public IPayloadReadClient<TInlineMetadata, TMetadata> CreateReadClient(Guid payloadId, Guid? fallbackBuildId = null)
        {
            return new MockPayloadClient<TInlineMetadata, TMetadata>(
                _payloadRepository ?? new MockListRepository(),
                _metadataRepository ?? new MockListRepository(),
                _dataRefRepository ?? new MockListRepository(),
                payloadId);
        }
    }
}
