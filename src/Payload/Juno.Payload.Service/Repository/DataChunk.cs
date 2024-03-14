using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Juno.Payload.Service.Repository
{
    public class DataChunk<TData>
    {
        public DataChunk(string continuationToken, IReadOnlyCollection<TData> items)
        {
            ContinuationToken = continuationToken;
            Items = items ?? throw new ArgumentNullException(nameof(items));
        }

        public string ContinuationToken { get; private set; }

        public IReadOnlyCollection<TData> Items { get; private set; }
    }
}
