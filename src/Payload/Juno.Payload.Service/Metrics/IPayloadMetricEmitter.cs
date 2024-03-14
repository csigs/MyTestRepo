using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Juno.Payload.Service.Metrics
{
    public interface IPayloadMetricEmitter
    {
        IDisposable BeginTimedOperation(string functionName);
        void EmitProcessingTime(string functionName, long processingTime);
    }
}
