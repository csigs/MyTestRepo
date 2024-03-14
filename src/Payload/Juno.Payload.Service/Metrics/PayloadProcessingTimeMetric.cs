using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Juno.Payload.Service.Metrics
{
    public class PayloadProcessingTimeMetric
    {
        public string CustomerId { get; set; }
        public string Region { get; set; }
        public long RawValue { get; set; }
    }
}
