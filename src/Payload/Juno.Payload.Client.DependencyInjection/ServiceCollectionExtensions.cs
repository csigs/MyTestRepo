using System;
using System.Net.Http;
using System.Runtime;

using Juno.Payload.Client.Abstraction;
using Juno.Payload.Client.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Localization.DelegatingHandlers;
using Microsoft.Localization.RestApiAccess;

namespace Juno.Payload.Client.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {

        /// <summary>
        /// Adds the Juno Payload Client to the service collection without any configuration.
        /// </summary>
        /// <param name="serviceDescriptors">service descripton</param>
        /// <returns>Instance of <see cref="IPayloadHttpClientBuilder"/></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IPayloadHttpClientBuilder AddPayloadClient(this IServiceCollection serviceDescriptors)
        {
            return RegisterCore(serviceDescriptors);
        }


        /// <summary>
        /// Registers the Juno Payload Client services with the DI container using options pattern bind to config.
        /// </summary>
        /// <param name="serviceDescriptors">service descriptors</param>
        /// <param name="config">config</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <returns>Instance of <see cref="IPayloadHttpClientBuilder"/></returns>
        public static IPayloadHttpClientBuilder AddPayloadClient(this IServiceCollection serviceDescriptors, IConfigurationRoot config, string PayloadClientConfigSectionName = nameof(PayloadClientOptions))
        {
            if (serviceDescriptors is null)
            {
                throw new ArgumentNullException(nameof(serviceDescriptors));
            }

            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var builder = RegisterCore(serviceDescriptors);

            serviceDescriptors.AddOptions<PayloadClientOptions>().Bind(config.GetSection(PayloadClientConfigSectionName));
            serviceDescriptors.AddSingleton(sp => sp.GetService<IOptions<PayloadClientOptions>>().Value);
            serviceDescriptors.AddSingleton(sp =>
            {
                var options = sp.GetService<IOptions<PayloadClientOptions>>().Value;
                return new PayloadClientConfig(options.ServiceUri, options.MSIScope);
            });

            return builder;
        }


        /// <summary>
        /// Registers the Juno Payload Client services with the DI container using <see cref="PayloadClientConfig"/> explicitly.
        /// </summary>
        /// <param name="serviceDescriptors">service descriptors</param>
        /// <param name="config">config</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static IPayloadHttpClientBuilder AddPayloadClient(this IServiceCollection serviceDescriptors, PayloadClientConfig payloadClientConfig)
        {
            if (serviceDescriptors is null)
            {
                throw new ArgumentNullException(nameof(serviceDescriptors));
            }

            var builder = RegisterCore(serviceDescriptors);
            builder.UsePayloadClientConfig(payloadClientConfig);
            return builder;
        }

        private static IPayloadHttpClientBuilder RegisterCore(IServiceCollection serviceDescriptors)
        {
            if (serviceDescriptors is null)
            {
                throw new ArgumentNullException(nameof(serviceDescriptors));
            }

            serviceDescriptors.AddSingleton<IPayloadApiClient>(s =>
            {
                var config = s.GetRequiredService<PayloadClientConfig>();
                return new PayloadApiClient(config);
            });
                        
            serviceDescriptors.AddSingleton(sp => (IPayloadApiV2Client)sp.GetService<IPayloadApiClient>());

            var httpClientBuilder = serviceDescriptors
                .AddHttpClient<IPayloadApiClient, PayloadApiClient>(
                    "payloadServiceAPI",
                    (s, options) =>
                    {
                        var config = s.GetRequiredService<PayloadClientConfig>();
                        //Might need revision during V2 testing. The http client needs to have a delegating handler that adds bearer token to authenticate with the API. 
                        //Maybe not use AddHttpClient method and rely set them up as singleton.
                        //AddHttpClient adds http client as transient and that could lead to port exhaustion during peak usage.
                        options = new HttpClient(
                            new ExponentialBackoffRetryDelegatingHandler(
                                new BearerTokenDelegatingHandler(new[] { config.MSIScope }, new HttpClientHandler())))
                        {
                            Timeout = PayloadApiClient.DefaultPayloadClientTimeOut
                        };
                    });

            return new PayloadHttpClientBuilder(httpClientBuilder);
        }
    }
}
