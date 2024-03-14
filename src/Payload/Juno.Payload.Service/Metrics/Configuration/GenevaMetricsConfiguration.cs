using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Juno.Common.Metrics.Contracts;

namespace Juno.Payload.Service.Metrics.Configuration
{
    public class GenevaMetricsConfiguration
    {
        public GenevaMetricsConfiguration(bool useMockGeneva, ICustomMetricObject[] customMetricObjects)
        {
            UseMockGeneva = useMockGeneva;
            CustomMetricObjects = customMetricObjects ?? throw new ArgumentNullException(nameof(customMetricObjects));
        }

        public bool UseMockGeneva { get; set; }

        public ICustomMetricObject[] CustomMetricObjects { get; private set; }
    }
}
