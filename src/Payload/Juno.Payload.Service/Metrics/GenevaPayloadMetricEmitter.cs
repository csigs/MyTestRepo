using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft;
using OpenTelemetry;
using OpenTelemetry.Exporter.Geneva;
using OpenTelemetry.Metrics;

namespace Juno.Payload.Service.Metrics
{
    public class GenevaPayloadMetricEmitter : IPayloadMetricEmitter
    {
        private static readonly string _processingTimeMetricName = "PayloadProcessingTime";
        private static readonly Meter _payloadMeter = new("Juno.Payload", "1.0");
        private static readonly Histogram<long> _payloadProcessingTimeHistogram = _payloadMeter.CreateHistogram<long>(_processingTimeMetricName);
        private readonly string _customerId;
        private readonly string _region;

        public GenevaPayloadMetricEmitter(string genevaConnectionString, string customerId, string region)
        {
            Requires.NotNullOrWhiteSpace(genevaConnectionString, nameof(genevaConnectionString));
            Requires.NotNullOrWhiteSpace(customerId, nameof(customerId));
            Requires.NotNullOrWhiteSpace(region, nameof(region));


            var meterProvider = Sdk.CreateMeterProviderBuilder()
                .AddMeter(_payloadMeter.Name)
                .AddGenevaMetricExporter(options =>
                {
                    options.ConnectionString = genevaConnectionString;
                })
                .Build();
            _customerId = customerId;
            _region = region;
        }

        public IDisposable BeginTimedOperation(string functionName)
        {
            Requires.NotNullOrWhiteSpace(functionName, nameof(functionName));
            return new TimedOperation(this, new KeyValuePair<string,string>("FunctionName", functionName));
        }

        public void EmitProcessingTime(string functionName, long processingTime)
        {
            Requires.NotNullOrWhiteSpace(functionName, nameof(functionName));
            _payloadProcessingTimeHistogram.Record(
               processingTime,
                    new("FunctionName", functionName),
                    new("CustomerId", _customerId),
                    new("Region", _region)
                );
        }
    }
}
