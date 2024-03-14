namespace Juno.Payload.Service.Repository
{
    using System.Threading;
    using System.Threading.Tasks;
    using Juno.Payload.Service.Model;
    using Microsoft.Localization.Juno.Common.Data.Repository;

    /// <summary>
    /// Interface for payload repository for dependency injection.
    /// </summary>
    public interface IPayloadRepository : ICosmosRepository
    {
    }
}
