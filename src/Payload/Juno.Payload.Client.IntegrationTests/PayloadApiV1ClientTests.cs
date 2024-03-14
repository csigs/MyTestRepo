using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

using Autofac;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Juno.Common;
using Juno.Common.Contracts.Eol;
using Juno.Common.DataReference;
using Juno.Common.DataReference.DataStoreService.AccessSource;
using Juno.Common.Metadata;
using Juno.LocManagementHub.Contracts.LocManifest;
using Juno.Payload.Client.Configuration;
using Juno.Payload.Handoff;
using Microsoft.Localization.SignProviders;
using Microsoft.Localization.SignProviders.Extensions;

namespace Juno.Payload.Client.IntegrationTests
{
    [TestClass]
   // disabling warnings for obsolete code with V1 client
#pragma warning disable 0612, 0618
    public class PayloadApiV1ClientTests
    {
        private IContainer _applicationContainer;

        #region Test data
        HandoffPayloadMetadata _inlineMetadata = new HandoffPayloadMetadata()
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
        #endregion

        [TestMethod]
        public async Task CreateDeletePayloadTest()
        {
            IConfigurationBuilder builder = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("integrationtests.settings.json", false, true);
            var config = builder.Build();
            Environment.SetEnvironmentVariable("AZURE_CLIENT_ID", config["AZURE_CLIENT_ID"]);
            Environment.SetEnvironmentVariable("AZURE_TENANT_ID", config["AZURE_TENANT_ID"]);
            Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", config["AZURE_CLIENT_SECRET"]);

            var apiClient = new PayloadApiClient(GetPayloadClientConfig());
            var payloadClient = await apiClient.CreatePayloadAsync<HandoffPayloadMetadata, LcgFullEolMap>();
            await apiClient.DeletePayloadAsync(payloadClient.PayloadId);
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task CreatePayloadInlineMetadataAndLoadTest(bool requireSign)
        {
            using var scope = _applicationContainer.BeginLifetimeScope();
            var signProvider = requireSign
                ? scope.Resolve<ISignProvider>()
                : null;

            var apiClient = new PayloadApiClient(GetPayloadClientConfig(), signProvider);
            var payloadClient = await apiClient.CreatePayloadAsync<HandoffPayloadMetadata, LcgFullEolMap>();

            await payloadClient.UpdatePayloadInlineMetadataAsync( _inlineMetadata, CancellationToken.None);
            var metadata = payloadClient.GetInlineMetadataAsync(CancellationToken.None);
            Assert.IsNotNull(metadata);
            await apiClient.DeletePayloadAsync(payloadClient.PayloadId);
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task CreatePayloadInlineMetadataIn1StepAndLoadTest(bool requireSign)
        {
            using var scope = _applicationContainer.BeginLifetimeScope();
            var signProvider = requireSign
                ? scope.Resolve<ISignProvider>()
                : null;

            var apiClient = new PayloadApiClient(GetPayloadClientConfig(), signProvider);
            var payloadClient = await apiClient.CreatePayloadWithDataAsync<HandoffPayloadMetadata, LcgFullEolMap>(_inlineMetadata);
            var metadata = payloadClient.GetInlineMetadataAsync(CancellationToken.None);
            Assert.IsNotNull(metadata);
            await apiClient.DeletePayloadAsync(payloadClient.PayloadId);
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task CreatePayloadMetadataAndLoadTest(bool requireSign)
        {
            using var scope = _applicationContainer.BeginLifetimeScope();
            var signProvider = requireSign
                ? scope.Resolve<ISignProvider>()
                : null;

            var apiClient = new PayloadApiClient(GetPayloadClientConfig(), signProvider);
            var payloadClient = await apiClient.CreatePayloadAsync<HandoffPayloadMetadata, LcgFullEolMap>();

            await payloadClient.UploadPayloadMetadataAsync(_payloadMetadata.Lcgs, false, CancellationToken.None);
            var metadata = await payloadClient.GetPayloadMetadataAsync(CancellationToken.None);
            Assert.IsNotNull(metadata);
            await apiClient.DeletePayloadAsync(payloadClient.PayloadId);
            await apiClient.DeletePayloadMetadataAsync(payloadClient.PayloadId);
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
            var payloadClient = await apiClient.CreatePayloadAsync<HandoffPayloadMetadata, LcgFullEolMap>();

            await payloadClient.UploadDataReferencesAsync(_dataReferenceDescriptors, false, CancellationToken.None);
            var dataReferences = await payloadClient.GetStoredLocElementDataReferencesAsync(CancellationToken.None);
            Assert.AreEqual(dataReferences.Count(), _dataReferenceDescriptors.Count());

            var dataReferencesReturned = await payloadClient.GetStoredLocElementDataReferencesAsync(_dataReferenceDescriptors.Select(d => d.LocElementMetadata), CancellationToken.None);
            await apiClient.DeletePayloadAsync(payloadClient.PayloadId);
            Assert.AreEqual(dataReferencesReturned.Count(), dataReferences.Count());
        }

        [TestMethod]
        public async Task CreatePayloadDataReferenceInTransactionAndLoadTest()
        {
            var apiClient = new PayloadApiClient(GetPayloadClientConfig());
            var payloadClient = await apiClient.CreatePayloadAsync<HandoffPayloadMetadata, LcgFullEolMap>();

            await payloadClient.UploadDataReferencesAsync(_dataReferenceDescriptors, true, CancellationToken.None);
            var dataReferences = await payloadClient.GetStoredLocElementDataReferencesAsync(CancellationToken.None);
            Assert.AreEqual(dataReferences.Count(), _dataReferenceDescriptors.Count());

            var dataReferencesReturned = await payloadClient.GetStoredLocElementDataReferencesAsync(_dataReferenceDescriptors.Select(d => d.LocElementMetadata), CancellationToken.None);
            await apiClient.DeletePayloadAsync(payloadClient.PayloadId);
            Assert.AreEqual(dataReferencesReturned.Count(), dataReferences.Count());
        }

        [TestMethod]
        [Ignore]
        public async Task DownloadLmhManifest()
        {
            var apiClient = new PayloadApiClient(GetPayloadClientConfig());
            var assembly = typeof(DataStoreServiceAccessSourceDescriptor).Assembly; // load assembly
            TypeRegistrar.Default.ReInitializeFromAssemblies(assembly);
            var payloadMetadata = 
                apiClient
                    .GetPayloadReadClient<SwLocManifestDto, SwLocManifestDto>(new Guid("95bfac4b-9beb-40c8-ac81-3bd8d5763125"), null);
            var result = await payloadMetadata.GetInlineMetadataAsync(CancellationToken.None);
            Assert.IsNotNull(result);
        }

        private PayloadClientConfig GetPayloadClientConfig()
        {
            //Do not put real credentials here. Use test settings in ci cd pipeline to pass secrets
            return new PayloadClientConfig("http://localhost:7071", "https://payloadpoc.juno.microsoft.com/.default");
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
    }
#pragma warning restore 0612, 0618
}
