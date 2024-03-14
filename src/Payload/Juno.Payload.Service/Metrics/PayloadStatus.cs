namespace Juno.Payload.Service.Metrics
{

    using System;

    using Microsoft;

    public class PayloadStatus
    {
        public string CustomerId { get; private set; }

        public string Instance { get; private set; }

        public int RawValue { get; private set; }

        public string StatusCode { get; private set; }

        public static PayloadStatus Create(IGenevaMetricDimensionProvider metricDimensionProvider, int rawValue, string statusCode)
        {
            Requires.NotNull(metricDimensionProvider, nameof(metricDimensionProvider));
            Requires.NotNullOrWhiteSpace(statusCode, nameof(statusCode));

            return new PayloadStatus
            {
                CustomerId = metricDimensionProvider.GetCustomerId(),
                Instance = metricDimensionProvider.GetInstance(Environment.MachineName),
                RawValue = rawValue,
                StatusCode = statusCode
            };
        }

        public static PayloadStatus Create(IGenevaMetricDimensionProvider metricDimensionProvider, int rawValue)
        {
            return Create(metricDimensionProvider, rawValue, rawValue.ToString());
        }
    }
}
