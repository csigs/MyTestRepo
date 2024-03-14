namespace Juno.Payload.Service.DI
{
    using Autofac;
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using Microsoft.Extensions.Configuration;

    using Microsoft.Localization.SignProviders;
    using Microsoft.Localization.SignProviders.Extensions;

    internal static class SignProviderExtensions
    {
        public static void AddSignProvider(this ContainerBuilder containerBuilder)
        {
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
        }
    }
}
