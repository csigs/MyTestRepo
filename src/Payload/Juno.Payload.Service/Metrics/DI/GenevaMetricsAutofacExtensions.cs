namespace Juno.Payload.Service.Metrics.DI
{
    using System;

    using Autofac;
    using Microsoft.Extensions.Configuration;
    using Moq;

    using Juno.Common.Metrics.Model;
    using Juno.Payload.Service.Metrics.Configuration;

    public static class GenevaMetricsAutofacExtensions
    {
        public static void AddGenevaMetrics(this ContainerBuilder containerBuilder, Action<GenevaMetricsConfigurationBuilder> metricConfiguration)
        {
            var metricsConfigurationBuilder = new GenevaMetricsConfigurationBuilder();
            metricConfiguration(metricsConfigurationBuilder);
            var customMetricsConfiguration = metricsConfigurationBuilder.Build();

            if(customMetricsConfiguration.UseMockGeneva)
            {
                // mock geneva logging (e.g. into ILogger only is not implemented yet that's why we will skip configuration)
                var metricDimProvider = Mock.Of<IGenevaMetricDimensionProvider>();
              
                containerBuilder
                    .Register(ctx => metricDimProvider)
                    .As<IGenevaMetricDimensionProvider>()
                    .SingleInstance();

                return;
            }

            containerBuilder
                .Register(ctx => new GenevaMetricDimensionProvider(ctx.Resolve<IConfigurationRoot>()))
                .As<IGenevaMetricDimensionProvider>()
                .SingleInstance();

            var customObjectMapBuilder = new CustomObjectMetricDefinitionMapper();

            foreach (var customMetricsObject in customMetricsConfiguration.CustomMetricObjects)
            {
                customObjectMapBuilder.AddCustomMetricDefinition(customMetricsObject);
            }

            var customObjectMap = customObjectMapBuilder.Build();
        }
    }
}
