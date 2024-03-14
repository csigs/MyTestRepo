using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Juno.Payload.Service.UnitTests.Data
{
    public class MockPayloadData
    {
        public Guid Id { get; set; }

        public IDictionary<string, string> Data { get; set; }        
    }
}
