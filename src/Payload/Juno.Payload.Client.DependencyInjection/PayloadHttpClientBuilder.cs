using System;
using System.Collections.Generic;
using System.Text;
using Juno.Payload.Client.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Juno.Payload.Client.DependencyInjection
{
    public class PayloadHttpClientBuilder : IPayloadHttpClientBuilder
    {
        private readonly IHttpClientBuilder _httpClientBuilder;

        public IServiceCollection Services { get { return this._httpClientBuilder.Services;  } }

        public string Name { get { return this._httpClientBuilder.Name; } }

        public PayloadHttpClientBuilder(IHttpClientBuilder httpClientBuilder)
        {
            this._httpClientBuilder = httpClientBuilder ?? throw new ArgumentNullException(nameof(httpClientBuilder));
        }

        public IPayloadHttpClientBuilder UsePayloadClientConfig(PayloadClientConfig payloadClientConfig)
        {
            if (payloadClientConfig is null)
            {
                throw new ArgumentNullException(nameof(payloadClientConfig));
            }

            Services.AddSingleton(payloadClientConfig);
            return this;
        }
    }
}
