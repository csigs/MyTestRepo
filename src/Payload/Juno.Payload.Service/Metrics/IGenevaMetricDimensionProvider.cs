
namespace Juno.Payload.Service.Metrics
{
    public interface IGenevaMetricDimensionProvider
    {
        string GetCustomerId();

        string GetInstance(string machineName);
    }
}
