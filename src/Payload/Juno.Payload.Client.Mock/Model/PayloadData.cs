
namespace Juno.Payload.Client.Mock.Model
{
    using Newtonsoft.Json;

    public class PayloadData<TData>
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("payloadId")]
        public string PayloadId { get; set; }

        [JsonProperty("providedId")]
        public string ProvidedId { get; set; }

        [JsonProperty("data")]
        public TData Data { get; set; }
    }
}
