namespace Juno.Payload.Client.Abstraction.V1
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines client interface for reading metadata from payload.
    /// </summary>
    /// <typeparam name="TInlineMetadata">Type of metadata.</typeparam>
    public interface IPayloadInlineMetadataReaderClient<TInlineMetadata> where TInlineMetadata : class
    {
        Task<TInlineMetadata> GetInlineMetadataAsync(CancellationToken cancellationToken);
    }
}
