namespace Juno.Payload.Client.Abstraction.V1
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Juno.Common.DataReference;
    using Juno.Common.Metadata;

    /// <summary>
    /// Interface defining client capability how to read data references assotiated with payload.
    /// </summary>
    public interface IPayloadLocDataReferenceReadClient
    {
        Task<IEnumerable<LocElementDataReferenceDescriptor>> GetStoredLocElementDataReferencesAsync(CancellationToken cancellationToken);

        Task<IEnumerable<LocElementDataReferenceDescriptor>> GetStoredLocElementDataReferencesAsync(IEnumerable<ILocElementMetadata> locElementMetadata, CancellationToken cancellationToken);

        Task<IEnumerable<LocElementDataReferenceDescriptor>> GetStoredLocElementDataReferencesAsync(IEnumerable<Guid> locElementIds, CancellationToken cancellationToken);
    }
}
