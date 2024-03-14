using Juno.Common.Metrics.Contracts;

namespace Juno.Payload.Service.Metrics.Configuration
{
    public interface IGenevaMetricObjectBuilder
    {
        ICustomMetricObject Build();
    }
}
