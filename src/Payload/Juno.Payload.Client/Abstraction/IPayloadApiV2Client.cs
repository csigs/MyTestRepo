namespace Juno.Payload.Client.Abstraction
{
    using System;
    using System.Threading.Tasks;

    using Juno.Payload.Dto;

    /// <summary>
    /// Interface providing basic functionality for interacting with the Juno Payload Service API v2 through underlying client interfaces <see cref="IPayloadClientV2"/>, <see cref="IPayloadDataClient{TPayloadData}"/>, <see cref="IPayloadMetadataCollectionClient{TPayloadMetadataCollectionItem}"/> and <see cref="IPayloadDataReferencesClient"/>.
    /// </summary>
    public interface IPayloadApiV2Client
    {
        /// <summary>
        /// Gets client for payload instance without creating payload representation recods on backend so payload still will need to be created through <see cref="IPayloadClientV2"/>
        /// </summary>
        /// <param name="payloadId">payload id which will be used through payload client</param>
        /// <param name="partitionKey">partition key which will be used through payload client</param>
        /// <returns></returns>
        IPayloadClientV2 GetPayloadClient(Guid payloadId, string partitionKey);

        /// <summary>
        /// Creates new payload record in payload store without payload data yet and provides client allowing to upload payload data, metadata collection and data references.
        /// </summary>
        /// <param name="partitionKey">partition key used for the payload.</param>
        /// <param name="category">category string which will be associated with the payload</param>
        /// <returns>Instance of payload client targeting specific payload</returns>
        Task<IPayloadClientV2> CreatePayloadV2Async<TPayloadData>(string partitionKey, string category = "Default")
            where TPayloadData : class;

        /// <summary>
        /// Creates new payload record in payload store together with uploading payload data in 1 step and provides client allowing to upload payload data, metadata collection and data references collection.
        /// </summary>
        /// <typeparam name="TPayloadData">Type of payload data (previously inline metadata) which will be associated with payload</typeparam>        
        /// <param name="partitionKey">partition key used for the payload.</param>
        /// <param name="category">category string which will be associated with the payload</param>
        /// <returns>Instance of payload client targeting specific payload</returns>
        Task<IPayloadClientV2> CreatePayloadWithDataV2Async<TPayloadData>(string partitionKey, TPayloadData payloadData, string category = "Default", PayloadDataStorageTypeDto payloadDataType = PayloadDataStorageTypeDto.BlobAttachment, TimeSpan uploadTimeout = default)
           where TPayloadData : class;

        /// <summary>
        /// Deletes payload in payload store and all underlying data.
        /// </summary>
        /// <param name="payloadId">Id of the payload.</param>
        /// <param name="partitionKey">Id of the payload.</param>
        /// <returns></returns>        
        Task DeletePayloadAsync(Guid payloadId, string partitionKey);
    }
}
