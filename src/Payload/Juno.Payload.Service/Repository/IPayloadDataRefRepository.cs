namespace Juno.Payload.Service.Repository
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Juno.Common.DataReference;
    using Juno.Payload.Service.Model;
    using Microsoft.Localization.Juno.Common.Data.Repository;

    /// <summary>
    /// Interface for payload data reference repository for dependency injection.
    /// </summary>
    public interface IPayloadDataRefRepository : ICosmosRepository
    {
    }
}
