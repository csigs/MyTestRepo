namespace Juno.Payload.Client.Abstraction.V1
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Juno.Common.DataReference;
    using Juno.Common.Metadata;

    /// <summary>
    /// Interface providing capability how to get read data accesses 
    /// </summary>
    [Obsolete]
    public interface IPayloadFallBackLocDataReferenceReadClient
    {
        Guid? FallbackBuildId { get; }

        Task<IEnumerable<LocElementDataReferenceDescriptor>> GetLocElementDataReferencesAsync(IEnumerable<ILocElementMetadata> locElements, CancellationToken cancellationToken);

        Task<IEnumerable<LocElementDataReferenceDescriptor>> GetLocElementDataReferencesAsync(IEnumerable<Guid> locElementIds, CancellationToken cancellationToken);
    }
}
