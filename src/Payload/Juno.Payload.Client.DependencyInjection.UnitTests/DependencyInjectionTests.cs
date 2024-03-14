using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;

using Juno.NewtonsoftHelp;
using Juno.Payload.Client.Abstraction;
using Juno.Payload.Client.Configuration;
using Juno.Payload.Client.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Juno.Payload.Client.DependencyInjection.UnitTests
{
    [TestClass]
    public class DependencyInjectionTests
    {
        [TestMethod]
        public void AddPayloadClientTest1()
        {
            var serviceCollection = new ServiceCollection();
            var config = new PayloadClientConfig("http://localhost:5000", "http://localhost:5000/.default");
            serviceCollection.AddPayloadClient(config);
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var payloadClientConfig = serviceProvider.GetService<PayloadClientConfig>();
            Assert.IsNotNull(payloadClientConfig);
            Assert.AreEqual(config.ServiceUri, payloadClientConfig.ServiceUri);
            
            // test can get payload api client v1
            var payloadApiClient = serviceProvider.GetService<IPayloadApiClient>();
            Assert.IsNotNull(payloadApiClient);

            // test can get payload api client v2
            var payloadApiV2Client = serviceProvider.GetService<IPayloadApiV2Client>();
            Assert.IsNotNull(payloadApiV2Client);
        }

        [TestMethod]
        public void AddPayloadClientTest2()
        {
            var config = new ConfigurationBuilder()
               .SetBasePath(Environment.CurrentDirectory)
               .AddJsonFile("unittest.settings.json", optional: true, reloadOnChange: true)
               .Build();
            
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddPayloadClient(config);
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var payloadClientConfig = serviceProvider.GetService<PayloadClientConfig>();
            Assert.IsNotNull(payloadClientConfig);
            Assert.IsNotNull(payloadClientConfig.ServiceUri);

            // test can get payload api client v1
            var payloadApiClient = serviceProvider.GetService<IPayloadApiClient>();
            Assert.IsNotNull(payloadApiClient);

            // test can get payload api client v2
            var payloadApiV2Client = serviceProvider.GetService<IPayloadApiV2Client>();
            Assert.IsNotNull(payloadApiV2Client);
        }

        [TestMethod]
        public void AddPayloadClientTest3()
        {
            var config = new ConfigurationBuilder()
               .SetBasePath(Environment.CurrentDirectory)
               .AddJsonFile("unittest.settings.json", optional: true, reloadOnChange: true)
               .Build();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddPayloadClient()
                .UsePayloadClientConfig(config.GetSection(nameof(PayloadClientOptions)).Get<PayloadClientOptions>().AsPayloadConfig())
                .UseDefaultRetryPolicy();
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var payloadClientConfig = serviceProvider.GetService<PayloadClientConfig>();
            Assert.IsNotNull(payloadClientConfig);
            Assert.IsNotNull(payloadClientConfig.ServiceUri);
            Assert.IsNotNull(payloadClientConfig.MSIScope);

            // test can get payload api client v1
            var payloadApiClient = serviceProvider.GetService<IPayloadApiClient>();
            Assert.IsNotNull(payloadApiClient);

            // test can get payload api client v2
            var payloadApiV2Client = serviceProvider.GetService<IPayloadApiV2Client>();
            Assert.IsNotNull(payloadApiV2Client);
        }

        [TestMethod]
        public void AddPayloadClientWithSignProviderTest()
        {
            var config = new ConfigurationBuilder()
               .SetBasePath(Environment.CurrentDirectory)
               .AddJsonFile("unittest.settings.json", optional: true, reloadOnChange: true)
               .Build();

            var serviceCollection = new ServiceCollection();
            var rsa = RSA.Create();
            var certificateRequest = new CertificateRequest("cn=foobar", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            var cert = certificateRequest.CreateSelfSigned(DateTime.Now, DateTime.Now.AddHours(1));
            var signProvider = new SignProvider(cert, hashAlgorithmName: HashAlgorithmName.SHA512, signaturePadding: RSASignaturePadding.Pkcs1);

            serviceCollection.AddSingleton<ISignProvider>(signProvider);

            serviceCollection.AddPayloadClient()
                .UsePayloadClientConfig(config.GetSection(nameof(PayloadClientOptions)).Get<PayloadClientOptions>().AsPayloadConfig())
                .UseDefaultRetryPolicy();
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var payloadClientConfig = serviceProvider.GetService<PayloadClientConfig>();
            Assert.IsNotNull(payloadClientConfig);
            Assert.IsNotNull(payloadClientConfig.ServiceUri);

            // test can get payload api client v1
            var payloadApiClient = serviceProvider.GetService<IPayloadApiClient>();
            Assert.IsNotNull(payloadApiClient);

            // test can get payload api client v2
            var payloadApiV2Client = serviceProvider.GetService<IPayloadApiV2Client>();
            Assert.IsNotNull(payloadApiV2Client);
        }
    }
}
