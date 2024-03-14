namespace Juno.Payload.Client.Abstraction
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Juno.Common.DataReference;

    /// <summary>
    /// Interface representing client for a payload scope defined by payload id <see cref="PayloadId"/> with partition key <see cref="PartitionKey"/>.
    /// Provides interface for payload service v2 access and underlying client for payload data manipulation through <see cref="IPayloadDataClient{TPayloadDataType}"/>,
    /// client for paylaod metadata collection manipulation <see cref="IPayloadMetadataCollectionClient{TMetadataCollectionItemType}"/> and data references access in a given payload scope through client
    /// <see cref="IPayloadDataReferencesClient"/>.
    /// </summary>
    public interface IPayloadClientV2
    {
        /// <summary>
        /// Gets Partition key used for given payload. Payload data is partitioned using this key and can't be accessed with incorrect partition key.
        /// </summary>
        string PartitionKey { get; }

        /// <summary>
        /// Gets id of the payload.
        /// </summary>
        Guid PayloadId { get; }

        /// <summary>
        /// Returns a flag payload v2 representation in the storage by a given payload id <see cref="PayloadId"/> with partition key <see cref="PartitionKey"/> already exists.
        /// </summary>
        /// <param name="category">requested category for payload</param>
        /// <returns>Instance of task</returns>
        Task<bool> ExistsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates payload v2 representation in the storage. If payload representation by a given payload id <see cref="PayloadId"/> with partition key <see cref="PartitionKey"/>  already exists, it will throw exception.
        /// </summary>
        /// <param name="category">requested category for payload</param>
        /// <returns>Instance of task</returns>
        Task CreateNewAsync(string category = "Default", CancellationToken cancellationToken = default);

        /// <summary>
        /// Completely deletes payload v2 representation in the storage together with all underlying data.
        /// </summary>
        /// <returns>Instance of task</returns>
        Task DeleteAsync(CancellationToken cancellationToken = default);


        /// <summary>
        /// Gets strongly typed client for working with payload data for a payload represented by <see cref="PartitionKey"/> and <see cref="PayloadId"/> enabling read and write operation for payload data.
        /// </summary>
        /// <typeparam name="TPayloadData">Type represeting payload data</typeparam>
        /// <returns>Instance of <see cref="IPayloadDataClient{TPayloadData}"/> for a given payload scope represented by the current <see cref="IPayloadClientV2"/></returns>
        IPayloadDataClient<TPayloadData> GetPayloadDataClient<TPayloadData>() where TPayloadData : class
;

        /// <summary>
        /// Gets strongly typed client for working with payload metadata collection for a payload represented by <see cref="PartitionKey"/> and <see cref="PayloadId"/> enabling read and write operation for payload metadata data collection.
        /// </summary>
        /// <typeparam name="TMetadataCollectionItem">Type represeting payload data</typeparam>
        /// <returns>Instance of <see cref="IPayloadMetadataCollectionClient<TMetadataCollectionItem>"/> for a given payload scope represented by the current <see cref="IPayloadClientV2"/></returns>
        IPayloadMetadataCollectionClient<TMetadataCollectionItem> GetPayloadMetadataCollectionClient<TMetadataCollectionItem>() where TMetadataCollectionItem : class
;

        /// <summary>
        /// Gets client for working with payload data references (library in namespace <see cref="Juno.Common.DataReference"/> <seealso cref="LocElementDataReferenceDescriptor"/> ) for a payload scope represented by <see cref="PartitionKey"/> and <see cref="PayloadId"/> enabling read and write operation for payload data references.
        /// </summary>
        /// <returns>Instance of <see cref="IPayloadDataReferencesClient"/> for a given payload scope represented by the current <see cref="IPayloadClientV2"/></returns>
        IPayloadDataReferencesClient GetPayloadDataReferencesClient();
    }
}
