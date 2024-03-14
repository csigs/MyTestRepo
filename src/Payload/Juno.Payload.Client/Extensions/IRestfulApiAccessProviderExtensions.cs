using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

using Microsoft.Localization.SignProviders;
using Microsoft.Localization.RestApiAccess;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace Juno.Payload.Client.Extensions
{
    internal static class IRestfulApiAccessProviderExtensions
    {
        internal static Task<HttpResponseMessage> PostJsonWithSignAsync(
            this IRestfulApiAccessProvider restfulApiAccessProvider,
            string functionUrl,
            object data,
            ISignProvider signProvider,
            CancellationToken cancellationToken = default)
        {
            return restfulApiAccessProvider.PostJsonWithSignAsync(functionUrl, JsonConvert.SerializeObject(data), signProvider, cancellationToken);
        }

        internal static Task<HttpResponseMessage> PostJsonWithSignAsync(
            this IRestfulApiAccessProvider restfulApiAccessProvider,
            string functionUrl,
            string jsonContent,
            ISignProvider signProvider,
            CancellationToken cancellationToken = default)
        {
            var headers = new Dictionary<string, StringValues>();

            if (signProvider != null)
            {
                var signature = signProvider.Sign(jsonContent, false);
                headers.Add(Constants.SignatureHttpHeader, signature);
            }

            return restfulApiAccessProvider.PostJsonAsync(functionUrl, jsonContent, headers, cancellationToken);
        }

        internal static Task<HttpResponseMessage> PutWithSignAsync(
           this IRestfulApiAccessProvider restfulApiAccessProvider,
           string functionUrl,
           object data,
           ISignProvider signProvider,
           CancellationToken cancellationToken = default)
        {
            return restfulApiAccessProvider.PutWithSignAsync(functionUrl, JsonConvert.SerializeObject(data), signProvider, cancellationToken);
        }

        internal static Task<HttpResponseMessage> PutWithSignAsync(
           this IRestfulApiAccessProvider restfulApiAccessProvider,
           string functionUrl,
           string content,
           ISignProvider signProvider,
           CancellationToken cancellationToken = default)
        {
            var headers = new Dictionary<string, StringValues>();

            var stringContent = new StringContent(content, System.Text.Encoding.UTF8, "application/json");
            if (signProvider != null)
            {
                var signature = signProvider.Sign(content, false);
                headers.Add(Constants.SignatureHttpHeader, signature);
            }

            return restfulApiAccessProvider.PutAsync(new Uri(functionUrl), stringContent, headers, cancellationToken);
        }
    }
}
