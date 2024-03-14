namespace Juno.Payload.Client.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Juno.Common.DataReference;
    using Juno.Common.Metadata;
    using Juno.Payload.Client.Abstraction;

    public static class PayloadDataReferencesClientExtensions
    {
        public static Task<IEnumerable<LocElementDataReferenceDescriptor>> GetLocElementDataReferencesAsync(this IPayloadDataReferencesClient dataReferencesClient, IEnumerable<ILocElementMetadata> locElements, CancellationToken cancellationToken = default)
        {
            if (dataReferencesClient is null)
            {
                throw new ArgumentNullException(nameof(dataReferencesClient));
            }

            if (locElements is null)
            {
                throw new ArgumentNullException(nameof(locElements));
            }

            return dataReferencesClient.ReadStoredLocElementDataReferencesChunkForAsync(locElements.Select(i => i.Id), cancellationToken);

        }

    }
}
