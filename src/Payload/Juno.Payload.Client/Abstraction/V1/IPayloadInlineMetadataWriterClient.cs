namespace Juno.Payload.Client.Abstraction.V1
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines client interface for writing the metadata.
    /// </summary>
    /// <typeparam name="TInlineMetadata">Type of the metadata</typeparam>
    public interface IPayloadInlineMetadataWriterClient<TInlineMetadata> where TInlineMetadata : class
    {
        Task UpdatePayloadInlineMetadataAsync(TInlineMetadata payloadMetadata, CancellationToken cancellationToken);
    }
}
