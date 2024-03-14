using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Juno.Payload.Service.Model
{
    /// <summary>
    /// Defines styles of payload data and how are stored
    /// </summary>
    public enum PayloadDataType
    {
        None = 0,
        
        /// <summary>
        /// Payload data is stored as json serialized data attachment in blob storage
        /// </summary>
        BlobAttachment = 1,

        /// <summary>
        /// Payload data is stored as inline payload metadata (json serialized) 
        /// </summary>
        InlineMetadata = 2,

        /// <summary>
        /// Payload data is stored as binary data attachment in blob storage
        /// </summary>
        BinaryBlobAttachment = 3,
    }
}
