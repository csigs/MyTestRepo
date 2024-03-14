
namespace Juno.Payload.Client.Mock.Model
{
    using Newtonsoft.Json;

    public class PayloadWithMetadata<TMetadata> : PayloadBase where TMetadata : class
    {
        public PayloadWithMetadata()
        {
            MetadataType = typeof(TMetadata).Name;
        }

        [JsonProperty("metadataType")]
        public string MetadataType { get; set; }

        [JsonProperty("metadata")]
        public TMetadata Metadata { get; set; }
    }
}
