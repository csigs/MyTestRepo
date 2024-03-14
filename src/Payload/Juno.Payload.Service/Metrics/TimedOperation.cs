using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Juno.Payload.Service.Metrics
{
    public class TimedOperation : IDisposable
    {
        private readonly Stopwatch _timer;
        private readonly IPayloadMetricEmitter _payloadMetricEmitter;
        private readonly KeyValuePair<string, string>[] _dimensions;

        public TimedOperation(IPayloadMetricEmitter payloadMetricEmitter, params KeyValuePair<string, string>[] dimensions)
        {
            _timer = Stopwatch.StartNew();
            _payloadMetricEmitter = payloadMetricEmitter;
            _dimensions = dimensions;
        }

        public void Dispose()
        {
            _timer.Stop();
            if (_dimensions.Any())
            {
                var functionName = _dimensions.First(d => d.Key.Equals("FunctionName")).Value;
                var processingTime = _timer.ElapsedMilliseconds;
                _payloadMetricEmitter.EmitProcessingTime(functionName, processingTime);
            }
            // else: no dimensions defined
        }
    }
}
