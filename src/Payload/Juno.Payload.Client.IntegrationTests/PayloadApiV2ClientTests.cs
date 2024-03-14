using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

using Autofac;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Juno.Common;
using Juno.Common.Contracts.Eol;
using Juno.Common.DataReference;
using Juno.Common.Metadata;
using Juno.Payload.Client.Configuration;
using Juno.Payload.Handoff;
using Microsoft.Localization.SignProviders;
using Microsoft.Localization.SignProviders.Extensions;

namespace Juno.Payload.Client.IntegrationTests
{
    [TestClass]
    public class PayloadApiV2ClientTests
    {
        public static IConfigurationRoot _config = new ConfigurationBuilder().AddJsonFile("integrationtests.settings.json", optional: true).Build();


        private static string _partitionKey = "TestPartition";

        private static string _category = "UnitTests";

        HandoffPayloadMetadata _payloadData = new HandoffPayloadMetadata()
        {
            Build = new Juno.Common.Contracts.BuildMetadata()
            {
                BuildLabel = "test",
                BuildId = Guid.NewGuid()
            },
            Branch = new Juno.Common.Contracts.BranchMetadata()
            {
                BranchId = Guid.NewGuid()
            }
        };

        FullEolMapMetadata _payloadMetadata = new FullEolMapMetadata()
        {
            Lcgs = new List<LcgFullEolMap>()
                    {
                        new LcgFullEolMap()
                        {
                            LcgMetadata = new Juno.Common.Contracts.LcgMetadata()
                            {
                                GroupId = Guid.NewGuid(),
                                Id = Guid.NewGuid(),
                                OriginalFileName = "test.lcg",
                                OriginalRelativePath = "test\\test.lcg",
                            },
                            EolMap = new List<EolMap>()
                            {
                                new EolMap()
                                {
                                    CultureName = "ko-KR",
                                    LclMetadata = new Juno.Common.Contracts.LclMetadata()
                                    {
                                        Id = Guid.NewGuid(),
                                        GroupId = Guid.NewGuid(),
                                        OriginalFileName = "test.lcl",
                                        OriginalRelativePath = "test\\test.lcl"
                                    }
                                }
                            }
                        }
                    }
        };
        private IContainer _applicationContainer;
        private readonly IEnumerable<LocElementDataReferenceDescriptor> _dataReferenceDescriptors = new List<LocElementDataReferenceDescriptor>()
        {
            new LocElementDataReferenceDescriptor()
            {
                DataAccessDescriptor = new Juno.Common.DataReference.AccessSource.DataAccessDescriptor()
                {
                    DataAccessSourceId = Guid.NewGuid(),
                    RelativeAccessPath = "test\\test.lcl"
                },
                LocElementMetadata = new Juno.Common.Contracts.LclMetadata()
                                        {
                                            Id = Guid.NewGuid(),
                                            GroupId = Guid.NewGuid(),
                                            OriginalFileName = "test.lcl",
                                            OriginalRelativePath = "test\\test.lcl"
                                        }.ToDefaultLocElementMetadata()
            },
            new LocElementDataReferenceDescriptor()
            {
                DataAccessDescriptor = new Juno.Common.DataReference.AccessSource.DataAccessDescriptor()
                {
                    DataAccessSourceId = Guid.NewGuid(),
                    RelativeAccessPath = "test\\test.lcg"
                },
                LocElementMetadata = new Juno.Common.Contracts.LcgMetadata()
                                {
                                    GroupId = Guid.NewGuid(),
                                    Id = Guid.NewGuid(),
                                    OriginalFileName = "test.lcg",
                                    OriginalRelativePath = "test\\test.lcg",
                                }.ToDefaultLocElementMetadata()
            },
            new LocElementDataReferenceDescriptor()
            {
                DataAccessDescriptor = new Juno.Common.DataReference.AccessSource.DataAccessDescriptor()
                {
                    DataAccessSourceId = Guid.NewGuid(),
                    RelativeAccessPath = "test\\test2.lcg"
                },
                LocElementMetadata = new Juno.Common.Contracts.LcgMetadata()
                                {
                                    GroupId = Guid.NewGuid(),
                                    Id = Guid.NewGuid(),
                                    OriginalFileName = "test2.lcg",
                                    OriginalRelativePath = "test\\test2.lcg",
                                }.ToDefaultLocElementMetadata()
            },
            new LocElementDataReferenceDescriptor()
            {
                DataAccessDescriptor = new Juno.Common.DataReference.AccessSource.DataAccessDescriptor()
                {
                    DataAccessSourceId = Guid.NewGuid(),
                    RelativeAccessPath = "test\\test3.lcg"
                },
                LocElementMetadata = new Juno.Common.Contracts.LcgMetadata()
                                {
                                    GroupId = Guid.NewGuid(),
                                    Id = Guid.NewGuid(),
                                    OriginalFileName = "test3.lcg",
                                    OriginalRelativePath = "test\\test3.lcg",
                                }.ToDefaultLocElementMetadata()
            },

        };

        [TestMethod]
        public void PayloadSerializationTest()
        {
            var json = JsonConvert.SerializeObject(_payloadData);
            var handoffPayload = JsonConvert.DeserializeObject<HandoffPayloadMetadata>(json);
            Assert.IsNotNull(handoffPayload);
            Assert.AreEqual(handoffPayload.PayloadId, _payloadData.PayloadId);
            Assert.AreEqual(handoffPayload.Branch.BranchId, _payloadData.Branch.BranchId);
        }

        [TestMethod]
        public async Task CreateDeletePayloadTest()
        {
            var apiClient = new PayloadApiClient(GetPayloadClientConfig());
            var payloadClient = await apiClient.CreatePayloadV2Async<HandoffPayloadMetadata>(_partitionKey, "UnitTests");
            await apiClient.DeletePayloadAsync(payloadClient.PayloadId, _partitionKey);
        }


        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task CreatePayloadWithPayloadDataIn1StepAndLoadTest(bool requireSign)
        {
            using var scope = _applicationContainer.BeginLifetimeScope();
            var signProvider = requireSign
                ? scope.Resolve<ISignProvider>()
                : null;

            var apiClient = new PayloadApiClient(GetPayloadClientConfig(), signProvider);
            var payloadClient = await apiClient.CreatePayloadWithDataV2Async(_partitionKey, _payloadData, _category);

            var payloadData = await payloadClient.GetPayloadDataClient<HandoffPayloadMetadata>()
                .ReadPayloadDataAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.IsNotNull(payloadData);
            Assert.AreEqual(_payloadData.PayloadId, payloadData.PayloadId);
            Assert.AreEqual(_payloadData.Branch.BranchId, payloadData.Branch.BranchId);
            Assert.AreEqual(_payloadData.Build.BuildId, payloadData.Build.BuildId);
            await payloadClient.DeleteAsync();
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task CreatePayloadInlineMetadataIn2StepAndLoadDeleteTest(bool requireSign)
        {
            using var scope = _applicationContainer.BeginLifetimeScope();
            var signProvider = requireSign
                ? scope.Resolve<ISignProvider>()
                : null;

            var apiClient = new PayloadApiClient(GetPayloadClientConfig(), signProvider);
            var payloadClient = apiClient.GetPayloadClient(Guid.NewGuid(), _partitionKey);
            await payloadClient.CreateNewAsync();
            var exists = await payloadClient.ExistsAsync();
            Assert.IsTrue(exists);
            await payloadClient.GetPayloadDataClient<HandoffPayloadMetadata>().UploadPayloadDataAsync(_payloadData);
            var payloadData = await payloadClient.GetPayloadDataClient<HandoffPayloadMetadata>().ReadPayloadDataAsync();
            Assert.IsNotNull(payloadData);
            Assert.AreEqual(_payloadData.PayloadId, payloadData.PayloadId);
            Assert.AreEqual(_payloadData.Branch.BranchId, payloadData.Branch.BranchId);
            Assert.AreEqual(_payloadData.Build.BuildId, payloadData.Build.BuildId);
            await payloadClient.DeleteAsync();
            exists = await payloadClient.ExistsAsync();
            Assert.IsFalse(exists);
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task CreatePayloadMetadataAsWholeAndLoadTest(bool requireSign)
        {
            using var scope = _applicationContainer.BeginLifetimeScope();
            var signProvider = requireSign
                ? scope.Resolve<ISignProvider>()
                : null;

            var apiClient = new PayloadApiClient(GetPayloadClientConfig(), signProvider);
            var payloadClient = apiClient.GetPayloadClient(Guid.NewGuid(), _partitionKey);
            await payloadClient.CreateNewAsync();

            await payloadClient.GetPayloadMetadataCollectionClient<LcgFullEolMap>().AddPayloadMetadataCollectionItemsAsync(_payloadMetadata.Lcgs, true, CancellationToken.None);
            var metadata = await payloadClient.GetPayloadMetadataCollectionClient<LcgFullEolMap>().ReadPayloadMetadataCollectionAsync().ToListAsync();
            Assert.IsNotNull(metadata);
            await payloadClient.GetPayloadMetadataCollectionClient<LcgFullEolMap>().DeleteMetadataCollectionAsync(CancellationToken.None);
            await payloadClient.DeleteAsync();
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task CreatePayloadDataReferenceAndLoadTest(bool requireSign)
        {
            using var scope = _applicationContainer.BeginLifetimeScope();
            var signProvider = requireSign
                ? scope.Resolve<ISignProvider>()
                : null;

            var apiClient = new PayloadApiClient(GetPayloadClientConfig(), signProvider);
            var payloadClient = apiClient.GetPayloadClient(Guid.NewGuid(), _partitionKey);
            await payloadClient.CreateNewAsync();

            await payloadClient.GetPayloadDataReferencesClient().AddLocElementDataReferencesAsync(_dataReferenceDescriptors, false);
            var dataReferences = await payloadClient.GetPayloadDataReferencesClient().ReadStoredLocElementDataReferencesAsync().ToListAsync();
            Assert.AreEqual(dataReferences.Count(), _dataReferenceDescriptors.Count());

            await payloadClient.GetPayloadDataReferencesClient().DeleteDataReferencesAsync();
            await payloadClient.DeleteAsync();
        }

        [TestMethod]
        public async Task CreatePayloadDataReferenceInTransactionAndLoadTest()
        {
            var apiClient = new PayloadApiClient(GetPayloadClientConfig());
            var payloadClient = apiClient.GetPayloadClient(Guid.NewGuid(), _partitionKey);
            await payloadClient.CreateNewAsync();

            await payloadClient.GetPayloadDataReferencesClient().AddLocElementDataReferencesAsync(_dataReferenceDescriptors, true);
            var dataReferences = await payloadClient.GetPayloadDataReferencesClient().ReadStoredLocElementDataReferencesAsync().ToListAsync();
            Assert.AreEqual(dataReferences.Count(), _dataReferenceDescriptors.Count());

            await payloadClient.GetPayloadDataReferencesClient().DeleteDataReferencesAsync();
            await payloadClient.DeleteAsync();
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task CreatePayloadDataReferenceAndLoadTest2(bool requireSign)
        {
            using var scope = _applicationContainer.BeginLifetimeScope();
            var signProvider = requireSign
                ? scope.Resolve<ISignProvider>()
                : null;

            var apiClient = new PayloadApiClient(GetPayloadClientConfig(), signProvider);
            var payloadClient = apiClient.GetPayloadClient(Guid.NewGuid(), _partitionKey);
            await payloadClient.CreateNewAsync();

            await payloadClient.GetPayloadDataReferencesClient().AddLocElementDataReferencesAsync(_dataReferenceDescriptors);
            var dataReferences = await payloadClient.GetPayloadDataReferencesClient().ReadStoredLocElementDataReferencesChunkForAsync(_dataReferenceDescriptors.Select(m => m.LocElementMetadata.GroupId).Distinct());
            Assert.AreEqual(dataReferences.Count(), _dataReferenceDescriptors.Count());

            await payloadClient.GetPayloadDataReferencesClient().DeleteDataReferencesAsync();
            await payloadClient.DeleteAsync();
        }

        [TestMethod]
        [Ignore("This test uses huge ammount of memory")]
        public async Task CanUploadDownloadLargePayload()
        {
            var apiClient = new PayloadApiClient( GetPayloadClientConfig());
            var payloadClient = apiClient.GetPayloadClient(Guid.NewGuid(), _partitionKey);
            await payloadClient.CreateNewAsync();
            var json = await File.ReadAllTextAsync(_config["LargePayloadFilePath"]);
            var jarray = JsonConvert.DeserializeObject<JArray>(json);
            await payloadClient.GetPayloadDataClient<JArray>().UploadPayloadDataAsync(jarray);
            var payloadData = await payloadClient.GetPayloadDataClient<JArray>().ReadPayloadDataAsync();
            Assert.AreEqual(jarray.Children().Count(), payloadData.Children().Count());
            await payloadClient.DeleteAsync();
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task CanUploadDownloadLargePayloadAsStream(bool requireSign)
        {
            using var scope = _applicationContainer.BeginLifetimeScope();
            var signProvider = requireSign
                ? scope.Resolve<ISignProvider>()
                : null;

            var apiClient = new PayloadApiClient(GetPayloadClientConfig(), signProvider);
            var payloadClient = apiClient.GetPayloadClient(Guid.NewGuid(), _partitionKey);
            await payloadClient.CreateNewAsync();
            var stream = File.Open(_config["LargePayloadFilePath"], FileMode.Open);
            await payloadClient.GetPayloadDataClient<Stream>().UploadPayloadDataAsync(stream);
            var payloadDataStream = await payloadClient.GetPayloadDataClient<Stream>().ReadPayloadDataAsync();
            Assert.AreEqual(stream.Length, payloadDataStream.Length);
            await payloadClient.DeleteAsync();
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task LargeMetadataCollectionReadTest(bool requireSign)
        {
            using var scope = _applicationContainer.BeginLifetimeScope();
            var signProvider = requireSign
                ? scope.Resolve<ISignProvider>()
                : null;

            //using existing pregenerated payload in POC environment
            var partitionKey = "branchid1";
            var payloadId = new Guid("9f87c4e5-c804-45a7-b885-3e4ec2501874");
            var apiClient = new PayloadApiClient(GetPayloadClientConfig(), signProvider);
            var payloadClient = apiClient.GetPayloadClient(payloadId, partitionKey);
            var metadataCollectionClient = payloadClient.GetPayloadMetadataCollectionClient<JObject>();
            var metadataCollection = await metadataCollectionClient.ReadPayloadMetadataCollectionAsync().ToListAsync();
            // keep the payload as it was pregenerated in POC
        }


        private PayloadClientConfig GetPayloadClientConfig()
        {
            //Do not put real credentials here. Use test settings in ci cd pipeline to pass secrets
            return new PayloadClientConfig("http://localhost:7071", "http://localhost:7071/.default");
        }

        /// <summary>
        /// Initialize test environment, setting up local environment variables that functions use.
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            var containerBuilder = new ContainerBuilder();

            var configBuilder = new ConfigurationBuilder();

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("integrationtests.settings.json", optional: true, reloadOnChange: true)
                .Build();

            configuration = configBuilder
                .AddAzureKeyVault(
                    new SecretClient(
                        new(configuration[Constants.PreReqKeyVaultUrlConfigName]), new DefaultAzureCredential()),
                    new AzureKeyVaultConfigurationOptions()
                    {
                        ReloadInterval = TimeSpan.FromMinutes(5)
                    })
                .Build();

            containerBuilder.Register(ctx => configuration)
                .As<IConfigurationRoot>()
                .InstancePerLifetimeScope();

            containerBuilder.Register(ctx =>
            {
                var config = ctx.Resolve<IConfigurationRoot>();
                var certSecret = config[Constants.MessageSignCertificateName]
                    ?? throw new KeyNotFoundException($"No configuration with name '{Constants.MessageSignCertificateName}'.");

                var cert = new X509Certificate2(Convert.FromBase64String(certSecret));
                var hashAlgorithmName = config[Constants.HashAlgorithmNameSecretName]?.GetHashAlgorithmName()
                    ?? HashAlgorithmName.SHA512;
                var signaturePadding = config[Constants.RSASignaturePaddingSecretName]?.GetRSASignaturePadding()
                    ?? RSASignaturePadding.Pkcs1;

                return new SignProvider(cert, hashAlgorithmName, signaturePadding);
            })
            .As<ISignProvider>()
            .InstancePerLifetimeScope();

            _applicationContainer = containerBuilder.Build();
        }

        [TestCleanup]
        public void TestCleanup()
        {
        }
    }
}
