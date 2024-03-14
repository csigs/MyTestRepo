namespace Juno.Payload.Client.Abstraction.V1
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface defines the whole payload read capability counting with specific metadata. Allowing to read metadata from payload, read associated data reference descriptors to the payload.
    /// And ability to read data reference descriptors with fallback mechanism to build snaphot layer if the data reference for given item was not found there.
    /// </summary>
    /// <typeparam name="TInlineMetadata">Inline Metadata type to read</typeparam>
    /// <typeparam name="TMetadata">Metadata type to read</typeparam>
    //[Obsolete]
    public interface IPayloadReadClient<TInlineMetadata, TMetadata> : IPayloadInlineMetadataReaderClient<TInlineMetadata>, IPayloadMetadataReadClient<TMetadata>, IPayloadLocDataReferenceReadClient, IPayloadFallBackLocDataReferenceReadClient
            where TInlineMetadata : class
            where TMetadata : class
    {
        Task<bool> ExistsAsync(CancellationToken cancellationToken);
    }
}
