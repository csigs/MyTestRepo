namespace Juno.Payload.Service.Repository
{
    using System;
    using System.IO;
    using System.Text;
    using Microsoft.Azure.Cosmos;
    using Newtonsoft.Json;


    /// <summary>
    /// The default Cosmos JSON.NET serializer.
    /// This class was coppied from Cosmos SDK repo as it's internal and we need to modify serializer properties to keep backwards compatibility.
    /// </summary>
    internal sealed class CosmosNewtonsoftJsonSerializer : CosmosSerializer
    {
        private static readonly Encoding DefaultEncoding = new UTF8Encoding(
            encoderShouldEmitUTF8Identifier: false,
            throwOnInvalidBytes: true);

        private readonly JsonSerializerSettings serializerSettings;

        /// <summary>
        /// Create a serializer that uses the JSON.net serializer
        /// </summary>
        /// <remarks>
        /// This is internal to reduce exposure of JSON.net types so
        /// it is easier to convert to System.Text.Json
        /// </remarks>
        internal CosmosNewtonsoftJsonSerializer(JsonSerializerSettings jsonSerializerSettings = null)
        {
            this.serializerSettings = jsonSerializerSettings;
        }

        /// <summary>
        /// Convert a Stream to the passed in type.
        /// </summary>
        /// <typeparam name="T">The type of object that should be deserialized</typeparam>
        /// <param name="stream">An open stream that is readable that contains JSON</param>
        /// <returns>The object representing the deserialized stream</returns>
        public override T FromStream<T>(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (typeof(Stream).IsAssignableFrom(typeof(T)))
            {
                return (T)(object)stream;
            }

            using (StreamReader sr = new StreamReader(stream))
            using (JsonTextReader jsonTextReader = new JsonTextReader(sr))
            {
                JsonSerializer jsonSerializer = this.GetSerializer();
                return jsonSerializer.Deserialize<T>(jsonTextReader);
            }
        }

        /// <summary>
        /// Converts an object to a open readable stream.
        /// </summary>
        /// <typeparam name="T">The type of object being serialized</typeparam>
        /// <param name="input">The object to be serialized</param>
        /// <returns>An open readable stream containing the JSON of the serialized object</returns>
        public override Stream ToStream<T>(T input)
        {
            MemoryStream streamPayload = new MemoryStream();
            using (StreamWriter streamWriter = new StreamWriter(streamPayload, encoding: DefaultEncoding, bufferSize: 1024, leaveOpen: true))
            using (JsonWriter writer = new JsonTextWriter(streamWriter))
            {
                writer.Formatting = Newtonsoft.Json.Formatting.None;
                JsonSerializer jsonSerializer = this.GetSerializer();
                jsonSerializer.Serialize(writer, input);
                writer.Flush();
                streamWriter.Flush();
            }

            streamPayload.Position = 0;
            return streamPayload;
        }

        /// <summary>
        /// JsonSerializer has hit a race conditions with custom settings that cause null reference exception.
        /// To avoid the race condition a new JsonSerializer is created for each call
        /// </summary>
        private JsonSerializer GetSerializer()
        {
            return JsonSerializer.Create(this.serializerSettings);
        }
    }
}
