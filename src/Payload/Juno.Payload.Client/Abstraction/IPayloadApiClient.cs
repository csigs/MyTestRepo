namespace Juno.Payload.Client.Abstraction
{
    using System;
    using System.Threading.Tasks;

    using Juno.Payload.Client.Abstraction.V1;

    /// <summary>
    /// Interface providing access to Payload service API.
    /// </summary>
    public interface IPayloadApiClient : IPayloadApiV2Client
    {
        /// <summary>
        /// Gets payload read client.
        /// </summary>
        /// <typeparam name="TInlineMetadata">Type of inline metadata i.e. branchId.</typeparam>
        /// <typeparam name="TMetadata">Type of metadata i.e. EolMap.</typeparam>
        /// <param name="payloadId">Payload ID.</param>
        /// <param name="fallbackBuildId">fallback build id to load data references in case they are not present in payload for a given metadata. If null latest build is used.</param>
        /// <returns>Instance of payload client targeting specific payload</returns>
        //[Obsolete("Use payload V2")]
        IPayloadReadClient<TInlineMetadata, TMetadata> GetPayloadReadClient<TInlineMetadata, TMetadata>(Guid payloadId, Guid? fallbackBuildId = null)
            where TInlineMetadata : class
            where TMetadata : class;

        /// <summary>
        /// Gets the payload client for given payload by payload ID.
        /// </summary>
        /// <typeparam name="TInlineMetadata">Type of inline metadata i.e. branchId.</typeparam>
        /// <typeparam name="TMetadata">Type of metadata i.e. EolMap.</typeparam>
        /// <param name="payloadId">payload id.</param>
        /// <param name="fallbackBuildId">fallback build id to load data references in case they are not present in payload for a given metadata. If null latest build is used.</param>
        /// <returns>Instance of payload client targeting specific payload</returns>        
        //[Obsolete("Use payload V2")]
        IPayloadClient<TInlineMetadata, TMetadata> GetPayloadClient<TInlineMetadata, TMetadata>(Guid payloadId, Guid? fallbackBuildId = null)
            where TInlineMetadata : class
            where TMetadata : class;

        /// <summary>
        /// Deletes payload in payload store.
        /// </summary>
        /// <param name="payloadId">Id of the payload.</param>
        /// <returns></returns>
        //[Obsolete("Use payload V2")]
        Task DeletePayloadAsync(Guid payloadId);

        /// <summary>
        /// Deletes payload metadata (metadata collection) in payload store associated to payload .
        /// </summary>
        /// <param name="payloadId">Id of the payload.</param>
        //[Obsolete("Use DeletePayloadMetadataCollectionAsync")]
        Task DeletePayloadMetadataAsync(Guid payloadId);

        /// <summary>
        /// Deletes payload metadata collection in payload store.
        /// </summary>
        /// <param name="payloadId">Id of the payload.</param>
        /// <param name="partitionKey">partition key used for the payload.</param>
        Task DeletePayloadMetadataCollectionAsync(Guid payloadId, string partitionKey);

        /// <summary>
        /// Deletes payload data references collection associated to the payload.
        /// </summary>
        /// <param name="payloadId">Id of the payload.</param>
        /// <param name="partitionKey">partition key used for the payload.</param>
        Task DeletePayloadDataReferencesAsync(Guid payloadId, string partitionKey);

        /// <summary>
        /// Creates new payload in payload store and provides client allowing to upload metadata and data references.
        /// </summary>
        /// <typeparam name="TInlineMetadata">Type of inline metadata (payload data) which will be associated with payload</typeparam>
        /// <typeparam name="TMetadata">Type of metadata item which will be stored in collection of metadata associated with payload i.e. EolMap.</typeparam>
        /// <returns>Instance of payload client targeting specific payload</returns>
        //[Obsolete("Use payload V2")]
        Task<IPayloadClient<TInlineMetadata, TMetadata>> CreatePayloadAsync<TInlineMetadata, TMetadata>()
            where TInlineMetadata : class
            where TMetadata : class;

        /// <summary>
        /// Creates new payload in payload store together with payload data and provides client allowing to upload metadata (metadata collection) and data references collection.
        /// </summary>
        /// <typeparam name="TInlineMetadata">Type of inline metadata (payload data) which will be associated with payload</typeparam>
        /// <typeparam name="TMetadata">Type of metadata item which will be stored in collection of metadata associated with payload i.e. EolMap.</typeparam>
        /// <returns>Instance of payload client targeting specific payload</returns>
        //[Obsolete("Use payload V2")]
        Task<IPayloadClient<TInlineMetadata, TMetadata>> CreatePayloadWithDataAsync<TInlineMetadata, TMetadata>(TInlineMetadata payloadMetadata, string category = "Default", TimeSpan uploadTimeout = default)
            where TInlineMetadata : class
            where TMetadata : class;
    }
}
