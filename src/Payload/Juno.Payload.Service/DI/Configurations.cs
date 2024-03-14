namespace Juno.Payload.Service.DI
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;
    using System.Security.Cryptography;
    using Autofac;
    using Azure.Extensions.AspNetCore.Configuration.Secrets;
    using Azure.Identity;
    using Azure.Security.KeyVault.Secrets;

    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    internal static class Configurations
    {
        public static void AddConfigurations(this ContainerBuilder containerBuilder)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var configBuilder = new ConfigurationBuilder();

            configuration = configBuilder
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .AddAzureKeyVault(
                    new SecretClient(
                        new(configuration[Constants.PreReqKeyVaultUrlConfigName]), new DefaultAzureCredential()),
                    new AzureKeyVaultConfigurationOptions()
                    {
                        ReloadInterval= TimeSpan.FromMinutes(5)
                    })
                .Build();

            containerBuilder.Register(ctx => configuration)
            .As<IConfigurationRoot>()
           .InstancePerLifetimeScope();
        }
    }
}
