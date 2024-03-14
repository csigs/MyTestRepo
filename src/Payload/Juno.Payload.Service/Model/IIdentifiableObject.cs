using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Juno.Payload.Service.Model
{
    public interface IIdentifiableObject
    {
        [JsonProperty("id")]
        string Id { get; set; } 
    }
}
