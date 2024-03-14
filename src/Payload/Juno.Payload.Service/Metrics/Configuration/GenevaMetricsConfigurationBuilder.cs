namespace Juno.Payload.Service.Metrics.Configuration;

using System;
using System.Collections.Generic;
using System.Linq;

public class GenevaMetricsConfigurationBuilder
{
    public bool _useMockGeneva;

    private readonly List<IGenevaMetricObjectBuilder> _metricBuilders = new List<IGenevaMetricObjectBuilder>();

    public GenevaMetricsConfigurationBuilder UseMockGeneva()
    {
        _useMockGeneva = true;
        return this;
    }

    public GenevaMetricsConfigurationBuilder AddMetric<TMetricSourceData>(Action<GenevaMetricItemConfigurationBuilder<TMetricSourceData>> configureMetric)
    {
        if (configureMetric is null)
        {
            throw new ArgumentNullException(nameof(configureMetric));
        }

        var metricBuilder = new GenevaMetricItemConfigurationBuilder<TMetricSourceData>();
        configureMetric(metricBuilder);
        _metricBuilders.Add(metricBuilder);
        return this;
    }

    public GenevaMetricsConfiguration Build()
    {
        return new GenevaMetricsConfiguration(_useMockGeneva, _metricBuilders.Select(b => b.Build()).ToArray());
    }
}
