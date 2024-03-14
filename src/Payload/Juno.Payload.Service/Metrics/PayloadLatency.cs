
namespace Juno.Payload.Service.Metrics
{
    public class PayloadLatency
    {
        public string CustomerId { get; set; }
        public string Region { get; set; }
        public string ResponseTime { get; set; }
        public long RawValue { get; set; }
    }
}
