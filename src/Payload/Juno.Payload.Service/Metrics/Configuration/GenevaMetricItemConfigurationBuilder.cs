using System;
using System.Linq.Expressions;
using Juno.Common.Metrics.Contracts;
using Juno.Common.Metrics.Model;

namespace Juno.Payload.Service.Metrics.Configuration
{
    public class GenevaMetricItemConfigurationBuilder<TMetricSourceData> : IGenevaMetricConfigurationBuilder<TMetricSourceData>, IGenevaMetricObjectBuilder
    {
        private readonly CustomMetricObjectMap<TMetricSourceData> _customMetricObjectMap;

        public GenevaMetricItemConfigurationBuilder()
        {
            _customMetricObjectMap = CustomObjectMapBuilder.Create<TMetricSourceData>();
        }

        public ICustomMetricObject Build()
        {
            return _customMetricObjectMap;
        }

        public IGenevaMetricConfigurationBuilder<TMetricSourceData> UseMetricName(string metricName)
        {
            _customMetricObjectMap.UseMetricName(metricName);
            return this;
        }

        public IGenevaMetricConfigurationBuilder<TMetricSourceData> WithMetricDimension(Expression<Func<TMetricSourceData, object>> metricFieldExpression, string dimensionName, int columnOrder = 0)
        {
            _customMetricObjectMap.WithMetricDimension(metricFieldExpression, dimensionName, columnOrder);
            return this;
        }

        public IGenevaMetricConfigurationBuilder<TMetricSourceData> WithRawValue(Expression<Func<TMetricSourceData, object>> rawValueField)
        {
            _customMetricObjectMap.WithRawValue(rawValueField);
            return this;
        }
    }
}
