using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Juno.Common.DataReference;
using Juno.Payload.Service.Model;
using Microsoft.Localization.Juno.Common.Data.Repository;

namespace Juno.Payload.Service.Repository
{
    public interface ICosmosRepository : IRepository
    {
        Task<DataChunk<T>> GetItemsChunkAsync<T>(Expression<Func<T, bool>> predicate, string partitionKey, string continuationToken = null, CancellationToken cancellationToken = default) where T : class;

        Task<IEnumerable<T>> GetItemsAsync<T>(Expression<Func<T, bool>> predicate, string partitionKey, CancellationToken cancellationToken = default) where T : class;

        Task DeleteItemsSteamAsync<T>(Expression<Func<T, bool>> predicate, string partitionKey, CancellationToken cancellationToken = default) where T : class, IIdentifiableObject;

        Task CreateItemsBatchAsync<T>(IEnumerable<T> enumerable, string partitionKey, CancellationToken cancellationToken = default) where T : class, IIdentifiableObject;
    }
}
