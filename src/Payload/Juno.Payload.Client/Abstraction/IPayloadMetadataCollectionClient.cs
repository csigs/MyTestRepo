namespace Juno.Payload.Client.Abstraction
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    //TODO: async enumerable or chunks return for items loading data from backend in chunks so that it does not load all at once which will not work for large collections 

    /// <summary>
    /// Defines payload metadata collection client
    /// </summary>
    /// <typeparam name="TPayloadMetadataCollectionItem"></typeparam>
    public interface IPayloadMetadataCollectionClient<TPayloadMetadataCollectionItem> where TPayloadMetadataCollectionItem : class
    {
        /// <summary>
        /// Gets partition key of a parent payload.
        /// </summary>
        string PayloadPartitionKey { get; }

        /// <summary>
        /// Gets parent payload id.
        /// </summary>
        Guid PayloadId { get; }

        /// <summary>
        /// Gets the whole metadata collection stored with the payload.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>Async enumerable of <typeparamref name="TPayloadMetadataCollectionItem"/>payload metadata item type</returns>
        IAsyncEnumerable<TPayloadMetadataCollectionItem> ReadPayloadMetadataCollectionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads the metadata collection stored with the payload by specific metadata ids.
        /// </summary>
        /// <param name="metadataItemIds">Enumeration metadata item ids</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Async enumerable of <typeparamref name="TPayloadMetadataCollectionItem"/>payload metadata item type</returns>
        Task<IEnumerable<TPayloadMetadataCollectionItem>> ReadPayloadMetadataCollectionForAsync(IEnumerable<Guid> metadataItemIds, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds payload metadata item into metadata collection. It always create new payload metadata items leaving the rest of the collection as it was before. So if you're trying to add metadata having Id property with the same value it will crash as metadata id for wrapping document is taking from there.
        /// </summary>
        /// <param name="metadata">List of payload metadata.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Instance of task</returns>
        Task AddPayloadMetadataCollectionItemsAsync(IEnumerable<TPayloadMetadataCollectionItem> metadata, bool useTransaction = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update payload metadata collection as a whole.
        /// </summary>
        /// <param name="metadata">List of payload metadata.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Instance of task</returns>
        Task UpdatePayloadMetadataCollectionItemsAsync(IEnumerable<TPayloadMetadataCollectionItem> metadata, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes payload metadata collection as a whole.
        /// </summary>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>Instance of task</returns>
        Task DeleteMetadataCollectionAsync(CancellationToken cancellationToken = default);
    }
}
