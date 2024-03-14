namespace Juno.Payload.Service.Metrics.DI
{
    using System;

    using Autofac;

    using Juno.Common.Metrics.Model;

    internal static class GenevaMetricExtensions
    {
        [Obsolete]
        public static void AddGenevaMetrics(this ContainerBuilder builder)
        {
            var payloadLatency = CustomObjectMapBuilder
               .Create<PayloadLatency>()
               .UseMetricName("PayloadLatency")
               .WithRawValue(c => c.RawValue)
               .WithMetricDimension(c => c.CustomerId, "PayloadLatency_CustomerId", 0)
               .WithMetricDimension(c => c.Region, "PayloadLatency_LocationId", 1)
               .WithMetricDimension(c => c.ResponseTime, "PayloadLatency_ResponseTime", 2);

            var payloadStatus = CustomObjectMapBuilder
               .Create<PayloadStatus>()
               .UseMetricName("PayloadStatus")
               .WithRawValue(c => c.RawValue)
               .WithMetricDimension(c => c.CustomerId, "PayloadStatus_CustomerId", 0)
               .WithMetricDimension(c => c.Instance, "PayloadStatus_Instance", 1)
               .WithMetricDimension(c => c.StatusCode, "PayloadStatus_StatusCode", 2);

            var customObjectMap = new CustomObjectMetricDefinitionMapper()
                .AddCustomMetricDefinition(payloadLatency)
                .AddCustomMetricDefinition(payloadStatus)
                .Build();
        }
    }
}
