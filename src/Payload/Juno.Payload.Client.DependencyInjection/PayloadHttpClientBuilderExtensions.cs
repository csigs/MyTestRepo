using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace Juno.Payload.Client.DependencyInjection
{
    public static class PayloadHttpClientBuilderExtensions
    {
        public static IPayloadHttpClientBuilder UseDefaultRetryPolicy(this IPayloadHttpClientBuilder payloadHttpClientBuilder, int retryAttempts = 3)
        {
            if (payloadHttpClientBuilder is null)
            {
                throw new ArgumentNullException(nameof(payloadHttpClientBuilder));
            }

            payloadHttpClientBuilder.AddPolicyHandler(GetDefaultPolicy(retryAttempts));
            return payloadHttpClientBuilder;
        }

        /// <summary>
        /// Gets the default retry policy for the Juno Payload Client with exponential backoff.
        /// </summary>
        /// <param name="retryAttempts">number of retries</param>
        /// <returns>Instance of async policy on http client resturning <see cref="HttpResponseMessage"/></returns>
        public static IAsyncPolicy<HttpResponseMessage> GetDefaultPolicy(int retryAttempts = 3)
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.BadRequest)
                .WaitAndRetryAsync(retryAttempts, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        }
    }
}
