namespace Juno.Payload.Client.Abstraction.V1
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Juno.Common.DataReference;

    /// <summary>
    /// Interface defines client capability to write data reference descriptors into associated payload.
    /// </summary>
    public interface IPayloadLocDataRefereceWriteClient
    {
        Task UploadDataReferencesAsync(IEnumerable<LocElementDataReferenceDescriptor> dataElements, bool useTransaction = false, CancellationToken cancellationToken = default);
    }
}
