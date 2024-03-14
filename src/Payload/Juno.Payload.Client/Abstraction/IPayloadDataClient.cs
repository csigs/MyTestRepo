namespace Juno.Payload.Client.Abstraction
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Juno.Payload.Dto;

    /// <summary>
    /// Gets interface representing access to payload data in a strongly typed manner.
    /// </summary>
    /// <typeparam name="TPayloadData"></typeparam>
    public interface IPayloadDataClient<TPayloadData>
    {
        /// <summary>
        /// Gets partition key of a parent payload.
        /// </summary>
        string PayloadPartitionKey { get; }

        /// <summary>
        /// Id of a payload scope.
        /// </summary>
        Guid PayloadId { get; }

        /// <summary>
        /// Reads the payload data from the payload store for a given payload scope.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>Instance of task returning payload data</returns>
        Task<TPayloadData> ReadPayloadDataAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Uploads and overwrites payload data for a given payload represented by this client.
        /// </summary>
        /// <param name="payloadData">payload data to upload</param>
        /// <param name="payloadDataStorageType">payload data storage type used when uploading this payload</param>
        /// <param name="uploadTimeout">timeout used for uploading payload data</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>Instance of task</returns>
        Task UploadPayloadDataAsync(TPayloadData payloadData, PayloadDataStorageTypeDto payloadDataStorageType = PayloadDataStorageTypeDto.BlobAttachment, TimeSpan uploadTimeout = default, CancellationToken cancellationToken = default);
    }
}
