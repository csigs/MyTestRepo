using Autofac;
using FakeItEasy;

using Juno.Common.DataReference;
using Juno.Payload.Client.Abstraction;
using Juno.Payload.Client.Mock.UnitTests.Data;

namespace Juno.Payload.Client.Mock.UnitTests
{
    [TestClass]
    public class MockPayloadApiClientTests
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private IContainer _applicationContainer;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        [TestMethod]
        public async Task CreateAndReadPayloadWithDataV2Async()
        {
            using var scope = _applicationContainer.BeginLifetimeScope();
            var payloadApiClient = scope.Resolve<IPayloadApiClient>();

            var expected = A.Dummy<MockPayloadData>();
            var partitionKey = Guid.NewGuid().ToString();

            var payloadClient = await payloadApiClient.CreatePayloadWithDataV2Async(partitionKey, expected)
                .ConfigureAwait(false);

            var actual = await payloadClient.GetPayloadDataClient<MockPayloadData>()
                .ReadPayloadDataAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public async Task CreateAndReadPayloadMetadataAsync()
        {
            using var scope = _applicationContainer.BeginLifetimeScope();
            var payloadApiClient = scope.Resolve<IPayloadApiClient>();

            var expectedPayload = A.Dummy<MockPayloadData>();
            var expectedPayloadMetadata = A.CollectionOfDummy<MockPayloadMetadata>(2);

            var partitionKey = Guid.NewGuid().ToString();

            var payloadClient = await payloadApiClient.CreatePayloadWithDataV2Async(partitionKey, expectedPayload)
                .ConfigureAwait(false);

            var metadataClient = payloadClient.GetPayloadMetadataCollectionClient<MockPayloadMetadata>();

            await metadataClient.AddPayloadMetadataCollectionItemsAsync(expectedPayloadMetadata)
                .ConfigureAwait(false);

            var actualPayloadMetadata = await metadataClient.ReadPayloadMetadataCollectionAsync()
                .ToListAsync()
                .ConfigureAwait(false);

            CollectionAssert.AreEqual(expectedPayloadMetadata.ToList(), actualPayloadMetadata.ToList());
        }

        [TestMethod]
        public async Task CreateAndReadPayloadAsync()
        {
            using var scope = _applicationContainer.BeginLifetimeScope();
            var payloadApiClient = scope.Resolve<IPayloadApiClient>();

            var expectedPayload = A.Dummy<MockPayloadData>();
            var expectedPayloadMetadata = A.CollectionOfDummy<MockPayloadMetadata>(2);

            var payloadClient = await payloadApiClient.CreatePayloadAsync<MockPayloadData, MockPayloadMetadata>()
                .ConfigureAwait(false);

            await payloadClient.UpdatePayloadInlineMetadataAsync(expectedPayload, CancellationToken.None)
                .ConfigureAwait(false); 

            await payloadClient.UploadPayloadMetadataAsync(expectedPayloadMetadata)
                .ConfigureAwait(false);

            var actualPayloadMetadata = await payloadClient.GetPayloadMetadataAsync(CancellationToken.None)
                .ConfigureAwait(false);

            CollectionAssert.AreEqual(expectedPayloadMetadata.ToList(), actualPayloadMetadata.ToList());
        }

        [TestMethod]
        public async Task AddPayloadDataReferencesAsync()
        {
            using var scope = _applicationContainer.BeginLifetimeScope();
            var payloadApiClient = scope.Resolve<IPayloadApiClient>();

            var expectedPayload = A.Dummy<MockPayloadData>();
            var expectedPayloadDataReferences = A.CollectionOfDummy<LocElementDataReferenceDescriptor>(2);

            var partitionKey = Guid.NewGuid().ToString();

            var payloadClient = await payloadApiClient.CreatePayloadWithDataV2Async(partitionKey, expectedPayload)
                .ConfigureAwait(false);

            var dataRefClient = payloadClient.GetPayloadDataReferencesClient();

            await dataRefClient.AddLocElementDataReferencesAsync(expectedPayloadDataReferences)
                .ConfigureAwait(false);

            var actualPayloadDataReferences = await dataRefClient.ReadStoredLocElementDataReferencesAsync()
                .ToListAsync()
                .ConfigureAwait(false);

            CollectionAssert.AreEqual(
                expectedPayloadDataReferences.ToList(),
                actualPayloadDataReferences.ToList(),
                new LocElementDataReferenceDescriptorComparer());
        }

        /// <summary>
        /// Initialize test environment, setting up local environment variables that functions use.
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterType<MockPayloadApiClient>().As<IPayloadApiClient>().SingleInstance();
            _applicationContainer = containerBuilder.Build();
        }
    }
}
