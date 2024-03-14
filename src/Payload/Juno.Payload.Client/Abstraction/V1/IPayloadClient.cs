namespace Juno.Payload.Client.Abstraction.V1
{
    using System;

    //[Obsolete("Use V2 instead")] commented to avoid SDL bugs
    public interface IPayloadClient<TInlineMetadata, TMetadata> : IPayloadReadClient<TInlineMetadata, TMetadata>, IPayloadInlineMetadataWriterClient<TInlineMetadata>, IPayloadMetadataWriteClient<TMetadata>, IPayloadLocDataRefereceWriteClient
        where TInlineMetadata : class
        where TMetadata : class
    {
        Guid PayloadId { get; }
    }
}
