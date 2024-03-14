namespace Juno.Payload.Service.Repository
{
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System;
    using Microsoft.Localization.Juno.Common.Data.Repository;


    /// <summary>
    /// Interface for payload metadata repository for dependency injection.
    /// </summary>
    public interface IPayloadMetadataRepository : ICosmosRepository
    {        
    }
}
