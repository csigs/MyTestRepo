

namespace Juno.Payload.Client.Abstraction
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface defining client capability how to read metadata assotiated with payload.
    /// </summary>
    public interface IPayloadMetadataReadClient<TMetadata> where TMetadata : class
    {
        Task<IEnumerable<TMetadata>> GetPayloadMetadataAsync(CancellationToken cancellationToken);

        Task<IEnumerable<TMetadata>> GetPayloadMetadataAsync(List<Guid> providedIds, CancellationToken cancellationToken);
    }
}
