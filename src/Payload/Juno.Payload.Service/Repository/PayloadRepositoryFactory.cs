namespace Juno.Payload.Service.Repository
{
    using Microsoft.Azure.Cosmos;
    using Microsoft.Azure.Documents.Client;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Serialization;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Provides functions to create payload repository instances.
    /// </summary>
    public static class PayloadRepositoryFactory
    {
        private static Lazy<CosmosClient> _innerClient = new Lazy<CosmosClient>(CreateClient);

        /// <summary>
        /// Database Id of payload store
        /// </summary>
        private static readonly string DatabaseId = Environment.GetEnvironmentVariable("PayloadDatabase");

        private static CosmosClientOptions GetCosmosClientOptions()
        {
            return new CosmosClientOptions()
            {
                ConnectionMode = ConnectionMode.Direct,
                Serializer = GetJsonSerializerSettings(),
                MaxRetryAttemptsOnRateLimitedRequests = 3,
                MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(5),
                ConsistencyLevel = ConsistencyLevel.Session
            };
        }

        private static CosmosSerializer GetJsonSerializerSettings()
        {
           return new CosmosNewtonsoftJsonSerializer(
                      new JsonSerializerSettings()
                      {
                          ContractResolver = new CamelCasePropertyNamesContractResolver(),
                          Converters = new List<JsonConverter> { new StringEnumConverter() }
                      });
        }

        internal static CosmosClient CreateClient()
        {
            var endpointUri = Environment.GetEnvironmentVariable("PayloadDocDbEndpointUri", EnvironmentVariableTarget.Process);
            var authKey = Environment.GetEnvironmentVariable("PayloadDocDbPrimaryKey", EnvironmentVariableTarget.Process);

            return new CosmosClient(
                endpointUri, 
                authKey,
                GetCosmosClientOptions());
        }

        public static IPayloadRepository CreatePayloadRepository()
        {
            return new PayloadCosmosDBRepository(_innerClient.Value, DatabaseId, Constants.PayloadCollectionId, "/partitionKey" );
        }

        public static IPayloadMetadataRepository CreatePayloadMetadataRepository()
        {
           return new PayloadCosmosDBRepository(_innerClient.Value, DatabaseId, Constants.DefaultMetadataCollectionId, "/payloadId");
        }

        public static IPayloadDataRefRepository CreatePayloadDataReferenceRepository()
        {
            return new PayloadCosmosDBRepository(_innerClient.Value, DatabaseId, Constants.DefaultDataReferenceCollectionId, "/payloadId");
        }
    }
}
