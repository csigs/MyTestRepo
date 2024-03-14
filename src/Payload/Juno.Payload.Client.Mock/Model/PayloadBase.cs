using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Juno.Payload.Client.Mock.Model
{
    /// <summary>
    /// Basic payload model that has essential data contract.
    /// </summary>
    public abstract class PayloadBase
    {
        /// <summary>
        /// Gets or sets the payload Id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the category of a payload.
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Gets or sets the created date of a payload.
        /// </summary>
        [JsonProperty("createdTime")]
        public DateTimeOffset CreatedTime { get; set; }

        /// <summary>
        /// Gets or sets the created date of a payload.
        /// </summary>
        [JsonProperty("updatedTime")]
        public DateTimeOffset UpdatedTime { get; set; }
    }
}
