namespace Juno.Payload.Client.Abstraction.V1
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface defines client capability to write metadata of a payload.
    /// </summary>
    public interface IPayloadMetadataWriteClient<TMetadata> where TMetadata : class
    {
        /// <summary>
        /// Upload payload metadata. It always create new payload metadata.
        /// </summary>
        /// <param name="metadata">List of payload metadata.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns></returns>
        Task UploadPayloadMetadataAsync(IEnumerable<TMetadata> metadata, bool useTransaction = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update payload metadata.
        /// </summary>
        /// <param name="metadata">List of payload metadata.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns></returns>
        Task UpdatePayloadMetadataAsync(IEnumerable<TMetadata> metadata, CancellationToken cancellationToken);
    }
}
