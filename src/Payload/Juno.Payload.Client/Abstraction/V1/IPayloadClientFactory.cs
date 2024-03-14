namespace Juno.Payload.Client.Abstraction.V1
{
    using System;

    //[Obsolete]
    public interface IPayloadClientFactory<TInlineMetadata, TMetadata>
        where TInlineMetadata : class
        where TMetadata : class
    {
        IPayloadClient<TInlineMetadata, TMetadata> Create(Guid payloadId, Guid? fallbackBuildId = null);

        IPayloadReadClient<TInlineMetadata, TMetadata> CreateReadClient(Guid payloadId, Guid? fallbackBuildId = null);
    }
}
