using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Juno.Payload.Service.Model
{
    public class PayloadDataReference<TData> : IIdentifiableObject
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
