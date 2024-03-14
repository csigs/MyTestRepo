namespace Juno.Payload.Client.Abstraction
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Juno.Common.DataReference;

    /// <summary>
    /// Interface defines client for manipulation with payload data references collection providing access to read/add and delete data from the collection.
    /// Typically data reference collection huge collection.
    /// </summary>
    public interface IPayloadDataReferencesClient
    {
        /// <summary>
        /// Gets partition key of a parent payload.
        /// </summary>
        string PayloadPartitionKey { get; }

        /// <summary>
        /// Gets parent payload id.
        /// </summary>
        Guid PayloadId { get; }

        IAsyncEnumerable<LocElementDataReferenceDescriptor> ReadStoredLocElementDataReferencesAsync(CancellationToken cancellationToken = default);

        Task<IEnumerable<LocElementDataReferenceDescriptor>> ReadStoredLocElementDataReferencesChunkForAsync(IEnumerable<Guid> locElementMetadataIds, CancellationToken cancellationToken = default);

        Task AddLocElementDataReferencesAsync(IEnumerable<LocElementDataReferenceDescriptor> dataReferenceDescriptors, bool useTransaction = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete all data references associated with payload.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task DeleteDataReferencesAsync(CancellationToken cancellationToken = default);
    }
}
