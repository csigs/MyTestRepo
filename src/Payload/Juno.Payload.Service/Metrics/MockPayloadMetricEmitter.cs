using System;
using System.Diagnostics;

namespace Juno.Payload.Service.Metrics
{
    public class MockPayloadMetricEmitter : IPayloadMetricEmitter
    {
        public IDisposable BeginTimedOperation(string functionName)
        {
            return new TimedOperation(this);
        }

        public void EmitProcessingTime(string functionName, long processingTime)
        {
            Trace.TraceInformation($"Function: '{functionName}' Processing Time: {processingTime}");
        }
    }
}
