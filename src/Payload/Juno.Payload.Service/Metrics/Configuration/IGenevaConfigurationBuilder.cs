using System.Linq.Expressions;
using System;
using Juno.Common.Metrics.Contracts;
using Juno.Common.Metrics.Model;

namespace Juno.Payload.Service.Metrics.Configuration
{
    public interface IGenevaMetricConfigurationBuilder<TMetricDataSource>
    {
        IGenevaMetricConfigurationBuilder<TMetricDataSource> UseMetricName(string metricName);


        IGenevaMetricConfigurationBuilder<TMetricDataSource> WithRawValue(Expression<Func<TMetricDataSource, object>> rawValueField);


        IGenevaMetricConfigurationBuilder<TMetricDataSource> WithMetricDimension(Expression<Func<TMetricDataSource, object>> metricFieldExpression, string dimensionName, int columnOrder = 0);
    }
}
